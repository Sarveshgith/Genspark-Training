CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TYPE bus_status AS ENUM ('pending', 'approved', 'rejected', 'inactive');
CREATE TYPE location_status AS ENUM ('pending', 'approved', 'rejected');
CREATE TYPE operator_status AS ENUM ('pending', 'approved', 'rejected', 'suspended');
CREATE TYPE passenger_gender AS ENUM ('male', 'female', 'other');
CREATE TYPE payment_status AS ENUM ('pending', 'success', 'failed');
CREATE TYPE route_status AS ENUM ('active', 'inactive');
CREATE TYPE seat_booking_status AS ENUM ('reserved', 'confirmed', 'cancelled');
CREATE TYPE ticket_status AS ENUM ('pending', 'confirmed', 'cancelled');
CREATE TYPE trip_status AS ENUM ('scheduled', 'active', 'completed', 'cancelled');
CREATE TYPE user_role AS ENUM ('user', 'operator', 'admin');

CREATE TABLE bus_layouts (
    id uuid NOT NULL,
    total_seats integer NOT NULL,
    config jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_bus_layouts" PRIMARY KEY (id)
);

CREATE TABLE locations (
    id uuid NOT NULL,
    city character varying(100) NOT NULL,
    state character varying(100) NOT NULL,
    status integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_locations" PRIMARY KEY (id)
);

CREATE TABLE users (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    email character varying(150) NOT NULL,
    phone character varying(15) NOT NULL,
    password_hash text NOT NULL,
    role integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_users" PRIMARY KEY (id)
);

CREATE TABLE operators (
    user_id uuid NOT NULL,
    license_number character varying(50) NOT NULL,
    status integer NOT NULL DEFAULT 0,
    approved_by uuid,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_operators" PRIMARY KEY (user_id),
    CONSTRAINT "FK_operators_users_approved_by" FOREIGN KEY (approved_by) REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT "FK_operators_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE routes (
    id uuid NOT NULL,
    from_id uuid NOT NULL,
    to_id uuid NOT NULL,
    status integer NOT NULL DEFAULT 0,
    created_by uuid,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_routes" PRIMARY KEY (id),
    CONSTRAINT ck_routes_from_to_different CHECK (from_id <> to_id),
    CONSTRAINT "FK_routes_locations_from_id" FOREIGN KEY (from_id) REFERENCES locations (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_routes_locations_to_id" FOREIGN KEY (to_id) REFERENCES locations (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_routes_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL
);

CREATE TABLE buses (
    id uuid NOT NULL,
    operator_id uuid NOT NULL,
    layout_id uuid NOT NULL,
    vehicle_number character varying(20) NOT NULL,
    status integer NOT NULL DEFAULT 0,
    approved_by uuid,
    approved_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_buses" PRIMARY KEY (id),
    CONSTRAINT "FK_buses_bus_layouts_layout_id" FOREIGN KEY (layout_id) REFERENCES bus_layouts (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_buses_operators_operator_id" FOREIGN KEY (operator_id) REFERENCES operators (user_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_buses_users_approved_by" FOREIGN KEY (approved_by) REFERENCES users (id) ON DELETE SET NULL
);

CREATE TABLE operator_locations (
    id uuid NOT NULL,
    operator_id uuid NOT NULL,
    location_id uuid NOT NULL,
    address text NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_operator_locations" PRIMARY KEY (id),
    CONSTRAINT "FK_operator_locations_locations_location_id" FOREIGN KEY (location_id) REFERENCES locations (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_operator_locations_operators_operator_id" FOREIGN KEY (operator_id) REFERENCES operators (user_id) ON DELETE CASCADE
);

CREATE TABLE trips (
    id uuid NOT NULL,
    bus_id uuid NOT NULL,
    route_id uuid NOT NULL,
    status integer NOT NULL DEFAULT 0,
    departure_time timestamp with time zone NOT NULL,
    arrival_time timestamp with time zone NOT NULL,
    price_per_seat numeric(10,2) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_trips" PRIMARY KEY (id),
    CONSTRAINT "FK_trips_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_trips_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE RESTRICT
);

CREATE TABLE tickets (
    id uuid NOT NULL,
    booking_ref character varying(50) NOT NULL,
    user_id uuid NOT NULL,
    trip_id uuid NOT NULL,
    status integer NOT NULL DEFAULT 0,
    base_amount numeric(10,2) NOT NULL,
    total_amount numeric(10,2) NOT NULL,
    payment_status integer NOT NULL DEFAULT 0,
    payment_ref character varying(100),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_tickets" PRIMARY KEY (id),
    CONSTRAINT "FK_tickets_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_tickets_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE TABLE seat_bookings (
    id uuid NOT NULL,
    ticket_id uuid NOT NULL,
    user_id uuid NOT NULL,
    trip_id uuid NOT NULL,
    seat_number character varying(10) NOT NULL,
    passenger_name character varying(100),
    passenger_age integer,
    passenger_gender integer,
    status integer NOT NULL DEFAULT 0,
    reserved_until timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_seat_bookings" PRIMARY KEY (id),
    CONSTRAINT "FK_seat_bookings_tickets_ticket_id" FOREIGN KEY (ticket_id) REFERENCES tickets (id) ON DELETE CASCADE,
    CONSTRAINT "FK_seat_bookings_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE,
    CONSTRAINT "FK_seat_bookings_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_buses_approved_by" ON buses (approved_by);

CREATE INDEX "IX_buses_layout_id" ON buses (layout_id);

CREATE INDEX "IX_buses_operator_id" ON buses (operator_id);

CREATE UNIQUE INDEX "IX_buses_vehicle_number" ON buses (vehicle_number);

CREATE UNIQUE INDEX "IX_locations_city_state" ON locations (city, state);

CREATE INDEX "IX_operator_locations_location_id" ON operator_locations (location_id);

CREATE INDEX "IX_operator_locations_operator_id" ON operator_locations (operator_id);

CREATE INDEX "IX_operators_approved_by" ON operators (approved_by);

CREATE INDEX "IX_routes_created_by" ON routes (created_by);

CREATE UNIQUE INDEX "IX_routes_from_id_to_id" ON routes (from_id, to_id);

CREATE INDEX "IX_routes_to_id" ON routes (to_id);

CREATE INDEX "IX_seat_bookings_ticket_id" ON seat_bookings (ticket_id);

CREATE UNIQUE INDEX "IX_seat_bookings_trip_id_seat_number" ON seat_bookings (trip_id, seat_number);

CREATE INDEX "IX_seat_bookings_user_id" ON seat_bookings (user_id);

CREATE UNIQUE INDEX "IX_tickets_booking_ref" ON tickets (booking_ref);

CREATE INDEX "IX_tickets_trip_id" ON tickets (trip_id);

CREATE INDEX "IX_tickets_user_id" ON tickets (user_id);

CREATE INDEX "IX_trips_bus_id" ON trips (bus_id);

CREATE INDEX "IX_trips_route_id" ON trips (route_id);

CREATE UNIQUE INDEX "IX_users_email" ON users (email);

CREATE UNIQUE INDEX "IX_users_phone" ON users (phone);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422095747_InitialCreate', '10.0.5');

COMMIT;

