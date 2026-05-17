CREATE OR REPLACE FUNCTION calculate_member_fine(member_id INT)
RETURNS DECIMAL
AS
$$
DECLARE
    total_fine DECIMAL;
BEGIN

    SELECT COALESCE(SUM("Amount"), 0)
    INTO total_fine
    FROM "Fines"
    WHERE "UserId" = member_id
    AND "IsPaid" = false;

    RETURN total_fine;

END;
$$
LANGUAGE plpgsql;