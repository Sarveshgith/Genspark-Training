-- 1. Shared function to update UpdatedAt column
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 2. Drop and recreate UpdatedAt triggers for all 10 tables
DROP TRIGGER IF EXISTS trg_Users_UpdatedAt ON "Users";
CREATE TRIGGER trg_Users_UpdatedAt BEFORE UPDATE ON "Users" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Roles_UpdatedAt ON "Roles";
CREATE TRIGGER trg_Roles_UpdatedAt BEFORE UPDATE ON "Roles" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Categories_UpdatedAt ON "Categories";
CREATE TRIGGER trg_Categories_UpdatedAt BEFORE UPDATE ON "Categories" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_MenuItems_UpdatedAt ON "MenuItems";
CREATE TRIGGER trg_MenuItems_UpdatedAt BEFORE UPDATE ON "MenuItems" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Tables_UpdatedAt ON "Tables";
CREATE TRIGGER trg_Tables_UpdatedAt BEFORE UPDATE ON "Tables" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Orders_UpdatedAt ON "Orders";
CREATE TRIGGER trg_Orders_UpdatedAt BEFORE UPDATE ON "Orders" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_OrderItems_UpdatedAt ON "OrderItems";
CREATE TRIGGER trg_OrderItems_UpdatedAt BEFORE UPDATE ON "OrderItems" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Bills_UpdatedAt ON "Bills";
CREATE TRIGGER trg_Bills_UpdatedAt BEFORE UPDATE ON "Bills" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_Items_UpdatedAt ON "Items";
CREATE TRIGGER trg_Items_UpdatedAt BEFORE UPDATE ON "Items" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS trg_MenuItemIngredients_UpdatedAt ON "MenuItemIngredients";
CREATE TRIGGER trg_MenuItemIngredients_UpdatedAt BEFORE UPDATE ON "MenuItemIngredients" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 3. Release Table when order completes (5) or is cancelled (6)
CREATE OR REPLACE FUNCTION trg_fn_release_table_on_order_complete()
RETURNS TRIGGER AS $$
BEGIN
    IF (NEW."Status" = 5 OR NEW."Status" = 6) AND (OLD."Status" IS DISTINCT FROM NEW."Status") THEN
        UPDATE "Tables"
        SET "Status" = 1 -- Available
        WHERE "Id" = NEW."TableId" AND "Status" <> 1;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_Orders_ReleaseTable ON "Orders";
CREATE TRIGGER trg_Orders_ReleaseTable
AFTER UPDATE OF "Status" ON "Orders"
FOR EACH ROW
EXECUTE FUNCTION trg_fn_release_table_on_order_complete();

-- 4. Check if a menu item can be prepared based on active ingredients and stock
CREATE OR REPLACE FUNCTION can_prepare_menu_item(p_menu_item_id INT)
RETURNS BOOLEAN AS $$
DECLARE
    v_has_ingredients BOOLEAN;
    v_cannot_prepare BOOLEAN;
BEGIN
    -- Check if it has any ingredients at all
    SELECT EXISTS (
        SELECT 1 FROM "MenuItemIngredients" WHERE "MenuItemId" = p_menu_item_id
    ) INTO v_has_ingredients;
    
    IF NOT v_has_ingredients THEN
        RETURN FALSE;
    END IF;

    -- Check if there are any ingredients where the item is inactive OR has insufficient stock
    SELECT EXISTS (
        SELECT 1 
        FROM "MenuItemIngredients" mii
        JOIN "Items" i ON mii."ItemId" = i."Id"
        WHERE mii."MenuItemId" = p_menu_item_id 
          AND (NOT i."IsActive" OR i."StockQuantity" < mii."QuantityRequired")
    ) INTO v_cannot_prepare;

    IF v_cannot_prepare THEN
        RETURN FALSE;
    END IF;

    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- 5. Reevaluate menu item availability function
CREATE OR REPLACE FUNCTION reevaluate_menu_item_availability(p_menu_item_id INT)
RETURNS VOID AS $$
DECLARE
    v_can_prepare BOOLEAN;
    v_is_manually_disabled BOOLEAN;
    v_should_be_available BOOLEAN;
BEGIN
    -- Get manual disable status
    SELECT "IsManuallyDisabled" INTO v_is_manually_disabled 
    FROM "MenuItems" 
    WHERE "Id" = p_menu_item_id;

    IF NOT FOUND THEN
        RETURN;
    END IF;

    v_can_prepare := can_prepare_menu_item(p_menu_item_id);

    IF v_can_prepare AND NOT v_is_manually_disabled THEN
        v_should_be_available := TRUE;
    ELSE
        v_should_be_available := FALSE;
    END IF;

    UPDATE "MenuItems"
    SET "IsAvailable" = v_should_be_available,
        "UpdatedAt" = NOW()
    WHERE "Id" = p_menu_item_id AND "IsAvailable" IS DISTINCT FROM v_should_be_available;
END;
$$ LANGUAGE plpgsql;

-- 6. Trigger function and trigger for when an Item changes
CREATE OR REPLACE FUNCTION trg_fn_reevaluate_menu_items_on_item_change()
RETURNS TRIGGER AS $$
DECLARE
    r RECORD;
    v_item_id INT;
BEGIN
    IF TG_OP = 'DELETE' THEN
        v_item_id := OLD."Id";
    ELSE
        v_item_id := NEW."Id";
    END IF;

    FOR r IN 
        SELECT DISTINCT "MenuItemId" 
        FROM "MenuItemIngredients" 
        WHERE "ItemId" = v_item_id
    LOOP
        PERFORM reevaluate_menu_item_availability(r."MenuItemId");
    END LOOP;

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    ELSE
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_Items_Reevaluate ON "Items";
CREATE TRIGGER trg_Items_Reevaluate
AFTER INSERT OR UPDATE OR DELETE ON "Items"
FOR EACH ROW
EXECUTE FUNCTION trg_fn_reevaluate_menu_items_on_item_change();

-- 7. Trigger function and trigger for when MenuItemIngredient changes
CREATE OR REPLACE FUNCTION trg_fn_reevaluate_menu_items_on_ingredient_change()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        PERFORM reevaluate_menu_item_availability(OLD."MenuItemId");
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        PERFORM reevaluate_menu_item_availability(NEW."MenuItemId");
        IF OLD."MenuItemId" IS DISTINCT FROM NEW."MenuItemId" THEN
            PERFORM reevaluate_menu_item_availability(OLD."MenuItemId");
        END IF;
        RETURN NEW;
    ELSE
        PERFORM reevaluate_menu_item_availability(NEW."MenuItemId");
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_MenuItemIngredients_Reevaluate ON "MenuItemIngredients";
CREATE TRIGGER trg_MenuItemIngredients_Reevaluate
AFTER INSERT OR UPDATE OR DELETE ON "MenuItemIngredients"
FOR EACH ROW
EXECUTE FUNCTION trg_fn_reevaluate_menu_items_on_ingredient_change();

-- 8. Trigger function and trigger for when MenuItem is inserted or updated
CREATE OR REPLACE FUNCTION trg_fn_menu_items_before_insert_or_update()
RETURNS TRIGGER AS $$
DECLARE
    v_can_prepare BOOLEAN;
BEGIN
    IF NEW."IsManuallyDisabled" THEN
        NEW."IsAvailable" := FALSE;
    ELSE
        v_can_prepare := can_prepare_menu_item(NEW."Id");
        NEW."IsAvailable" := v_can_prepare;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_MenuItems_BeforeInsertOrUpdate ON "MenuItems";
CREATE TRIGGER trg_MenuItems_BeforeInsertOrUpdate
BEFORE INSERT OR UPDATE ON "MenuItems"
FOR EACH ROW
EXECUTE FUNCTION trg_fn_menu_items_before_insert_or_update();
