using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Hubs;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.HubTests;

[TestFixture]
public class RestaurantHubTest
{
    private Mock<ILogger<RestaurantHub>> _loggerMock = null!;
    private Mock<ISignalService> _signalServiceMock = null!;
    private Mock<HubCallerContext> _contextMock = null!;
    private Mock<IHubCallerClients> _clientsMock = null!;
    private Mock<IGroupManager> _groupsMock = null!;
    private RestaurantHub _hub = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RestaurantHub>>();
        _signalServiceMock = new Mock<ISignalService>();
        _contextMock = new Mock<HubCallerContext>();
        _clientsMock = new Mock<IHubCallerClients>();
        _groupsMock = new Mock<IGroupManager>();

        _hub = new RestaurantHub(_loggerMock.Object, _signalServiceMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object
        };
    }

    private static ClaimsPrincipal CreateUserPrincipal(string name, string role, string sessionType = "Staff", int? userId = null, int? tableId = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
            new Claim("SessionType", sessionType)
        };

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (tableId.HasValue)
        {
            claims.Add(new Claim("tableId", tableId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    #region OnConnectedAsync Tests

    [Test]
    public async Task OnConnectedAsync_UnauthenticatedClient_DoesNotAddToGroups()
    {
        // Arrange
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-1");
        _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal)null!);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_GuestClient_AddsToTableGroup()
    {
        // Arrange
        var user = CreateUserPrincipal("Guest-5", "Customer", "Guest", tableId: 5);
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-guest");
        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync("conn-guest", "table-5", default), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_ChefClient_AddsToKitchenGroup()
    {
        // Arrange
        var user = CreateUserPrincipal("ChefJohn", "Chef", "Staff");
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-chef");
        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync("conn-chef", "kitchen", default), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WaiterClient_AddsToWaiterGroups()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", "Staff", userId: 12);
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-waiter");
        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync("conn-waiter", "waiter-12", default), Times.Once);
        _groupsMock.Verify(g => g.AddToGroupAsync("conn-waiter", "waiters", default), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_AdminClient_AddsToAdminsGroup()
    {
        // Arrange
        var user = CreateUserPrincipal("AdminAlice", "Admin", "Staff");
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-admin");
        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync("conn-admin", "admins", default), Times.Once);
    }

    #endregion

    #region SendKitchenMessage Tests

    [Test]
    public void SendKitchenMessage_Unauthenticated_ThrowsHubException()
    {
        // Arrange
        _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal)null!);
        var jsonPayload = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.ThrowsAsync<HubException>(async () => 
            await _hub.SendKitchenMessage("cook_fast", jsonPayload));
    }

    [Test]
    public void SendKitchenMessage_UnauthorizedRole_ThrowsHubException()
    {
        // Arrange
        var user = CreateUserPrincipal("ChefJohn", "Chef");
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.ThrowsAsync<HubException>(async () => 
            await _hub.SendKitchenMessage("cook_fast", jsonPayload));
    }

    [Test]
    public void SendKitchenMessage_InvalidType_ThrowsHubException()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", userId: 12);
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.ThrowsAsync<HubException>(async () => 
            await _hub.SendKitchenMessage("invalid_type", jsonPayload));
    }

    [Test]
    public async Task SendKitchenMessage_ValidRequest_SendsMessageSuccessfully()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", userId: 12);
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{\"orderId\":42,\"tableId\":5,\"note\":\"Hurry please\"}").RootElement;

        // Act
        await _hub.SendKitchenMessage("cook_fast", jsonPayload);

        // Assert
        _signalServiceMock.Verify(s => s.SendKitchenMessageAsync(It.Is<HubNotificationDto>(n =>
            n.Channel == "Kitchen" &&
            n.Type == "cook_fast" &&
            n.Title == "Cook Fast Request" &&
            n.Message == "Table #5 requested to prepare order #42 faster." &&
            n.OrderId == 42 &&
            n.TableId == 5
        )), Times.Once);
    }

    [Test]
    public async Task SendKitchenMessage_CooldownViolated_ThrowsHubException()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", userId: 12);
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{\"orderId\":99,\"tableId\":5}").RootElement;

        // Act - First request succeeds
        await _hub.SendKitchenMessage("cook_fast", jsonPayload);

        // Act & Assert - Second request within 2 minutes throws HubException
        var ex = Assert.ThrowsAsync<HubException>(async () => 
            await _hub.SendKitchenMessage("cook_fast", jsonPayload));
        
        Assert.That(ex.Message, Contains.Substring("cooldown"));
    }

    #endregion

    #region SendFloorMessage Tests

    [Test]
    public void SendFloorMessage_UnauthorizedRole_ThrowsHubException()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", userId: 12);
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.ThrowsAsync<HubException>(async () => 
            await _hub.SendFloorMessage("order_ready_reminder", 5, jsonPayload));
    }

    [Test]
    public async Task SendFloorMessage_ValidRequest_SendsMessageSuccessfully()
    {
        // Arrange
        var user = CreateUserPrincipal("ChefJohn", "Chef");
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{\"orderId\":10}").RootElement;

        // Act
        await _hub.SendFloorMessage("order_ready_reminder", 5, jsonPayload);

        // Assert
        _signalServiceMock.Verify(s => s.SendFloorMessageAsync(It.Is<HubNotificationDto>(n =>
            n.Channel == "Floor" &&
            n.Type == "order_ready_reminder" &&
            n.OrderId == 10 &&
            n.TableId == 5
        )), Times.Once);
    }

    #endregion

    #region SendAdminAlert Tests

    [Test]
    public async Task SendAdminAlert_ValidRequest_SendsAlertSuccessfully()
    {
        // Arrange
        var user = CreateUserPrincipal("ChefJohn", "Chef");
        _contextMock.Setup(c => c.User).Returns(user);
        var jsonPayload = JsonDocument.Parse("{\"itemId\":3,\"itemName\":\"Cheese\",\"currentQuantity\":2}").RootElement;

        // Act
        await _hub.SendAdminAlert("low_stock_critical", jsonPayload);

        // Assert
        _signalServiceMock.Verify(s => s.SendAdminAlertAsync(It.Is<HubNotificationDto>(n =>
            n.Channel == "Admin" &&
            n.Type == "low_stock_critical" &&
            n.ItemName == "Cheese"
        )), Times.Once);
    }

    #endregion

    #region GuestCallWaiter Tests

    [Test]
    public void GuestCallWaiter_NonGuestSession_ThrowsHubException()
    {
        // Arrange
        var user = CreateUserPrincipal("WaiterSam", "Waiter", "Staff");
        _contextMock.Setup(c => c.User).Returns(user);

        // Act & Assert
        Assert.ThrowsAsync<HubException>(async () => 
            await _hub.GuestCallWaiter("assistance"));
    }

    [Test]
    public async Task GuestCallWaiter_ValidGuestSession_SendsFloorMessage()
    {
        // Arrange
        var user = CreateUserPrincipal("Guest-4", "Customer", "Guest", tableId: 4);
        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _hub.GuestCallWaiter("bill");

        // Assert
        _signalServiceMock.Verify(s => s.SendFloorMessageAsync(It.Is<HubNotificationDto>(n =>
            n.Channel == "Floor" &&
            n.Type == "guest_call_bill" &&
            n.TableId == 4 &&
            n.Priority == "high"
        )), Times.Once);
    }

    #endregion
}
