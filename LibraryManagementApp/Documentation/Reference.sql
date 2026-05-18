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

CREATE OR REPLACE FUNCTION get_available_books_by_category(category_id_param INT)
RETURNS TABLE(
    book_id INT,
    title TEXT,
    author TEXT,
    category_name TEXT,
    available_copies BIGINT
)
AS
$$
BEGIN
    RETURN QUERY

    SELECT
        b."Id",
        b."Title",
        b."Author",
        c."CategoryName",
        COUNT(bc."Id")

    FROM "Books" b

    JOIN "Categories" c
        ON b."CategoryId" = c."Id"

    JOIN "BookCopies" bc
        ON b."Id" = bc."BookId"

    WHERE
        b."CategoryId" = category_id_param
        AND bc."Status" = 0

    GROUP BY
        b."Id",
        b."Title",
        b."Author",
        c."CategoryName";
END;
$$
LANGUAGE plpgsql;