-- ==========================================
-- Reports Database Setup (PostgreSQL)
-- Location: Data/Reports_Setup.sql
-- ==========================================

-- 1. Daily Revenue Function (fn_daily_revenue)
-- GET /api/reports/revenue/daily?date=YYYY-MM-DD
CREATE OR REPLACE FUNCTION fn_daily_revenue(p_date DATE)
RETURNS TABLE (
    "TotalOrders" INT,
    "TotalRevenue" DECIMAL(18,2),
    "AvgOrderValue" DECIMAL(18,2),
    "CancelledOrders" INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COALESCE(COUNT(o."Id")::INT, 0) AS "TotalOrders",
        COALESCE(SUM(CASE WHEN o."Status" = 5 THEN o."TotalAmount" ELSE 0.0 END), 0.0)::DECIMAL(18,2) AS "TotalRevenue",
        COALESCE(AVG(CASE WHEN o."Status" = 5 THEN o."TotalAmount" ELSE NULL END), 0.0)::DECIMAL(18,2) AS "AvgOrderValue",
        COALESCE(COUNT(CASE WHEN o."Status" = 6 THEN 1 END)::INT, 0) AS "CancelledOrders"
    FROM "Orders" o
    WHERE (o."CreatedAt" AT TIME ZONE 'UTC')::DATE = p_date;
END;
$$ LANGUAGE plpgsql;


-- 2. Range Revenue Query uses generate_series cross joined with fn_daily_revenue


-- 3. Orders Summary Function (fn_order_summary)
-- GET /api/reports/orders/summary?from=YYYY-MM-DD&to=YYYY-MM-DD
CREATE OR REPLACE FUNCTION fn_order_summary(p_from DATE, p_to DATE)
RETURNS TABLE (
    "TotalOrders" INT,
    "CompletedOrders" INT,
    "CancelledOrders" INT,
    "CancellationRate" DECIMAL(18,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(o."Id")::INT AS "TotalOrders",
        COUNT(CASE WHEN o."Status" = 5 THEN 1 END)::INT AS "CompletedOrders",
        COUNT(CASE WHEN o."Status" = 6 THEN 1 END)::INT AS "CancelledOrders",
        CASE 
            WHEN COUNT(o."Id") = 0 THEN 0.0::DECIMAL(18,2)
            ELSE (COUNT(CASE WHEN o."Status" = 6 THEN 1 END)::DECIMAL * 100.0 / COUNT(o."Id"))::DECIMAL(18,2)
        END AS "CancellationRate"
    FROM "Orders" o
    WHERE (p_from IS NULL OR (o."CreatedAt" AT TIME ZONE 'UTC')::DATE >= p_from)
      AND (p_to IS NULL OR (o."CreatedAt" AT TIME ZONE 'UTC')::DATE <= p_to);
END;
$$ LANGUAGE plpgsql;


-- 4. Top Selling Items View (vw_top_selling_items)
-- GET /api/reports/menu/top-items?limit=N
CREATE OR REPLACE VIEW vw_top_selling_items AS
SELECT 
    mi."Name" AS "ItemName",
    c."Name" AS "Category",
    COALESCE(SUM(oi."Quantity"), 0)::INT AS "TotalQtySold",
    COALESCE(SUM(oi."Quantity" * oi."UnitPrice"), 0.0)::DECIMAL(18,2) AS "TotalRevenue"
FROM "OrderItems" oi
JOIN "MenuItems" mi ON oi."MenuItemId" = mi."Id"
JOIN "Categories" c ON mi."CategoryId" = c."Id"
JOIN "Orders" o ON oi."OrderId" = o."Id"
WHERE o."Status" = 5 -- Completed
GROUP BY mi."Name", c."Name";


-- 5. Category Performance View (vw_category_performance)
-- GET /api/reports/menu/category-performance
CREATE OR REPLACE VIEW vw_category_performance AS
SELECT 
    c."Name" AS "CategoryName",
    COUNT(DISTINCT o."Id")::INT AS "OrderCount",
    COALESCE(SUM(oi."Quantity" * oi."UnitPrice"), 0.0)::DECIMAL(18,2) AS "TotalRevenue"
FROM "Categories" c
LEFT JOIN "MenuItems" mi ON mi."CategoryId" = c."Id"
LEFT JOIN "OrderItems" oi ON oi."MenuItemId" = mi."Id"
LEFT JOIN "Orders" o ON oi."OrderId" = o."Id" AND o."Status" = 5
GROUP BY c."Name";


-- 6. Kitchen SLA View (vw_kitchen_sla)
-- GET /api/reports/kitchen/sla
CREATE OR REPLACE VIEW vw_kitchen_sla AS
WITH OrderPrepTimes AS (
    SELECT 
        o."Id" AS "OrderId",
        o."CreatedAt",
        o."CompletedAt",
        MAX(mi."PreparationTime") AS "MaxPrepTimeMinutes"
    FROM "Orders" o
    JOIN "OrderItems" oi ON oi."OrderId" = o."Id"
    JOIN "MenuItems" mi ON oi."MenuItemId" = mi."Id"
    WHERE o."Status" = 5 AND o."CompletedAt" IS NOT NULL
    GROUP BY o."Id", o."CreatedAt", o."CompletedAt"
),
SLACalculation AS (
    SELECT 
        "OrderId",
        CASE 
            WHEN EXTRACT(EPOCH FROM ("CompletedAt" - "CreatedAt")) / 60.0 <= "MaxPrepTimeMinutes" THEN 1
            ELSE 0
        END AS "WithinSLA"
    FROM OrderPrepTimes
)
SELECT 
    COALESCE(COUNT(CASE WHEN "WithinSLA" = 1 THEN 1 END)::INT, 0) AS "WithinSLA",
    COALESCE(COUNT(CASE WHEN "WithinSLA" = 0 THEN 1 END)::INT, 0) AS "BreachedSLA",
    CASE 
        WHEN COUNT(*) = 0 THEN 0.0::DECIMAL(18,2)
        ELSE (COUNT(CASE WHEN "WithinSLA" = 1 THEN 1 END)::DECIMAL * 100.0 / COUNT(*))::DECIMAL(18,2)
    END AS "SLAPercentage"
FROM SLACalculation;


-- 7. Table Turnover Function (fn_table_turnover)
-- GET /api/reports/tables/turnover?date=YYYY-MM-DD
CREATE OR REPLACE FUNCTION fn_table_turnover(p_date DATE)
RETURNS TABLE (
    "TableId" INT,
    "TableNumber" INT,
    "CompletedOrdersCount" INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        t."Id" AS "TableId",
        t."Number" AS "TableNumber",
        COUNT(o."Id")::INT AS "CompletedOrdersCount"
    FROM "Tables" t
    LEFT JOIN "Orders" o ON o."TableId" = t."Id" AND o."Status" = 5 AND (o."CompletedAt" AT TIME ZONE 'UTC')::DATE = p_date
    WHERE t."IsDeleted" = false
    GROUP BY t."Id", t."Number";
END;
$$ LANGUAGE plpgsql;
