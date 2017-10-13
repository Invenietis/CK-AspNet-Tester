using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.AspNet.Tester.Tests
{
    /// <summary>
    /// Useless service. Here to be injected in the <see cref="StupidMiddleware"/>.
    /// </summary>
    public class StupidService
    {
        /// <summary>
        /// Gets the current Utc time.
        /// </summary>
        /// <returns>The current time.</returns>
        public string GetText() => $"It is {DateTime.UtcNow}.";
    }
}
