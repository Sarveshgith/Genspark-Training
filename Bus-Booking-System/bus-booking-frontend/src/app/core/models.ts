export interface AuthResponse {
  accessToken: string;
  expiresInSeconds: number;
  userId: string;
  role: string;
  email: string;
}

export interface RouteSummary {
  id: string;
  fromId: string;
  toId: string;
  status: string;
  from: { city: string; state: string };
  to: { city: string; state: string };
}

export interface TripSummary {
  id: string;
  busId: string;
  routeId: string;
  busVehicleNumber?: string;
  routeLabel?: string;
  status: string;
  departureTime: string;
  arrivalTime: string;
  pricePerSeat: number;
  createdAt: string;
}

export interface LayoutSeatConfig {
  seatNumber: string;
  femaleOnly: boolean;
}

export interface LayoutConfig {
  rows: number;
  cols: number;
  seats: LayoutSeatConfig[];
}
