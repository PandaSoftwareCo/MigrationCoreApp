using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationCoreApp.Interfaces
{
    public interface IAppSettings
    {
        string DefaultConnection { get; set; }
        string SecondaryConnection { get; set; }
    }
}
