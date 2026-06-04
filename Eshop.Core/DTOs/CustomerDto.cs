namespace Eshop.Core.DTOs
{
    public class CustomerRegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class CustomerLoginRequestDto
    {
        public string Email { get; set; } = string.Empty; // Οι πελάτες κάνουν login με Email, όχι Username!
        public string Password { get; set; } = string.Empty;
    }

    public class CustomerAuthResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class CustomerRefreshRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}