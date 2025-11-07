namespace OrderManagement.Configuration
{
    public class JwtOptions
    {
        /// <summary>
        /// Expected issuer of access tokens.
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Optional audience; leave empty to disable audience validation.
        /// </summary>
        public string? Audience { get; set; }

        /// <summary>
        /// Symmetric signing key material.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Access token lifetime in minutes.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 8 * 60;

        /// <summary>
        /// Allowed server/client clock drift in minutes when validating tokens.
        /// </summary>
        public int ClockSkewMinutes { get; set; } = 2;
    }
}
