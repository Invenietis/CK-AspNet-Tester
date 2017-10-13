using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.AspNet.Tester
{
    /// <summary>
    /// Very basic wrapper around a process.
    /// </summary>
    public class ExternalProcess
    {
        readonly Action<ProcessStartInfo> _configure;
        readonly Action<Process> _softStop;
        readonly object _simpleLock;
        Process _p;

        /// <summary>
        /// Initialize a new ExternalProcess object.
        /// </summary>
        /// <param name="configure">Requires configuration function.</param>
        /// <param name="softStop">Optional function that knows how to politely ask the process to stop.</param>
        public ExternalProcess( Action<ProcessStartInfo> configure, Action<Process> softStop = null )
        {
            if( configure == null ) throw new ArgumentNullException( nameof( configure ) );
            _configure = configure;
            _softStop = softStop;
            _simpleLock = new object();
        }

        /// <summary>
        /// Ensures that the process is running.
        /// This is thread safe (a simple lock is used).
        /// </summary>
        public void EnsureRunning()
        {
            lock( _simpleLock )
            {
                if( _p != null && _p.HasExited )
                {
                    _p.Dispose();
                    _p = null;
                }
                if( _p == null )
                {
                    var pI = new ProcessStartInfo();
                    _configure( pI );
                    _p = Process.Start( pI );
                }
            }
        }

        /// <summary>
        /// Gets whether this process is running.
        /// </summary>
        public bool IsRunning => _p != null && !_p.HasExited;

        /// <summary>
        /// Stops the process.
        /// This is thread safe (a simple lock is used).
        /// </summary>
        public void StopAndWaitForExit()
        {
            lock( _simpleLock )
            {
                if( _p != null )
                {
                    if( !_p.HasExited )
                    {
                        _softStop?.Invoke( _p );
                        if( !_p.WaitForExit( 200 ) ) _p.Kill();
                        _p.WaitForExit( 200 );
                    }
                    _p.WaitForExit();
                    _p.Dispose();
                    _p = null;
                }
            }
        }

    }
}
