namespace KidsDictionaryApi.Models
{
    /// <summary>
    /// Represents a parent's account identified by their email address.
    /// Only the email is stored — no child personal information is collected.
    /// </summary>
    public class UserAccount
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}
