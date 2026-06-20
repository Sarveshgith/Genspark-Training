export interface LoginModel{
    email: string;
    password: string;
}

export interface RegisterModel{
    email: string;
    name: string;
    password: string;
    roleId: number;
    phoneNumber: string;
    address: string | null;
}

export interface UserModel{
    id: number;
    email: string;
    name: string;
    roleName: string;
    phoneNumber: string | null;
    address: string | null;
}

export interface LoginResponseModel{
    token: string;
    refreshToken: string;
    user: UserModel;
}