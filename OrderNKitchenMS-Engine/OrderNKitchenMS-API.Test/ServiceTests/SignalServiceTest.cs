using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Hubs;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class SignalServiceTest
{
    private Mock<IHubContext<RestaurantHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;
    private Mock<ILogger<SignalService>> _loggerMock = null!;
    private SignalService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _hubContextMock = new Mock<IHubContext<RestaurantHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _loggerMock = new Mock<ILogger<SignalService>>();

        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);

        _service = new SignalService(_hubContextMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task NotifyNewOrderAsync_SendsToKitchenGroup()
    {
        var order = new OrderDto { Id = 101 };
        await _service.NotifyNewOrderAsync(order);

        _hubClientsMock.Verify(c => c.Group("kitchen"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveNewOrder", It.Is<object[]>(args => args[0] == order), default), Times.Once);
    }

    [Test]
    public async Task NotifyOrderUpdateAsync_SendsToTableAndKitchenGroups()
    {
        var tracking = new GuestOrderTrackingDto { OrderId = 101 };
        await _service.NotifyOrderUpdateAsync(5, tracking);

        _hubClientsMock.Verify(c => c.Group("table-5"), Times.Once);
        _hubClientsMock.Verify(c => c.Group("kitchen"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveOrderUpdate", It.Is<object[]>(args => args[0] == tracking), default), Times.Exactly(2));
    }

    [Test]
    public async Task NotifyTablesUpdatedAsync_SendsToAll()
    {
        await _service.NotifyTablesUpdatedAsync();

        _hubClientsMock.Verify(c => c.All, Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveTableStateUpdate", It.IsAny<object[]>(), default), Times.Once);
    }

    [Test]
    public async Task NotifyBillGeneratedAsync_SendsToTableGroup()
    {
        var bill = new BillDto { Id = 202 };
        await _service.NotifyBillGeneratedAsync(5, bill);

        _hubClientsMock.Verify(c => c.Group("table-5"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("bill_generated", It.Is<object[]>(args => args[0] == bill), default), Times.Once);
    }

    [Test]
    public async Task NotifyBillPaidAsync_SendsToTableGroup()
    {
        var bill = new BillDto { Id = 202 };
        await _service.NotifyBillPaidAsync(5, bill);

        _hubClientsMock.Verify(c => c.Group("table-5"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("bill_paid", It.Is<object[]>(args => args[0] == bill), default), Times.Once);
    }

    [Test]
    public async Task NotifyGuestSessionEndedAsync_SendsToTableGroup()
    {
        await _service.NotifyGuestSessionEndedAsync(5);

        _hubClientsMock.Verify(c => c.Group("table-5"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("GuestSessionEnded", It.IsAny<object[]>(), default), Times.Once);
    }

    [Test]
    public async Task SendKitchenMessageAsync_SendsToKitchenGroup()
    {
        var notif = new HubNotificationDto { Title = "Alert" };
        await _service.SendKitchenMessageAsync(notif);

        _hubClientsMock.Verify(c => c.Group("kitchen"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveKitchenMessage", It.Is<object[]>(args => args[0] == notif), default), Times.Once);
    }

    [Test]
    public async Task SendFloorMessageAsync_SendsToWaitersGroup()
    {
        var notif = new HubNotificationDto { Title = "Alert" };
        await _service.SendFloorMessageAsync(notif);

        _hubClientsMock.Verify(c => c.Group("waiters"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveFloorMessage", It.Is<object[]>(args => args[0] == notif), default), Times.Once);
    }

    [Test]
    public async Task SendAdminAlertAsync_SendsToAdminsGroup()
    {
        var notif = new HubNotificationDto { Title = "Alert" };
        await _service.SendAdminAlertAsync(notif);

        _hubClientsMock.Verify(c => c.Group("admins"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveAdminAlert", It.Is<object[]>(args => args[0] == notif), default), Times.Once);
    }
}
