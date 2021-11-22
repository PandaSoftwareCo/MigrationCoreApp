using MigrationCoreApp.Interfaces;

namespace MigrationCoreApp.Models
{
    public class AppSettings : IAppSettings
    {
        public string DefaultConnection { get; set; }
        public string SecondaryConnection { get; set; }
    }
}
