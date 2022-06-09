using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.Core.Model
{
    /// <summary>
    /// This enum is used to indicate whether the client app should retrieve a given data set from the "main" endpoint (/dataset/etc) 
    /// or the "Enso" endpoint (/reference/enso/etc). It may go away at some point if we unify the two endpoints.
    /// </summary>
    public enum Endpoints
    {
        Main,
        Enso
    }
}
