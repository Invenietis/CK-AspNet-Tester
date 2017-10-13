using System;
using System.IO;
using System.Reflection;

namespace CK.AspNet.Tester.Tests
{
    /// <summary>
    /// Centralized helper functions that offers a Monitor, monitoring Logs initialization
    /// and simple database management.
    /// </summary>
    public static class TestHelper
    {
        static string _binFolder;
        static string _projectFolder;
        static string _solutionFolder;
        static string _repositoryFolder;
        static string _logFolder;
        static string _currentTestProjectName;
        static string _buildConfiguration;

        static TestHelper()
        {
        }

        /// <summary>
        /// Gets the path to the project folder.
        /// </summary>
        public static string ProjectFolder
        {
            get
            {
                if( _projectFolder == null ) InitalizePaths();
                return _projectFolder;
            }
        }

        /// <summary>
        /// Gets the path to the root folder: where the .git folder is.
        /// </summary>
        public static string RepositoryFolder
        {
            get
            {
                if( _repositoryFolder == null ) InitalizePaths();
                return _repositoryFolder;
            }
        }


        /// <summary>
        /// Gets the solution folder. It is the parent directory of the 'Tests/' folder (that must exist).
        /// </summary>
        static public string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
        }

        /// <summary>
        /// Gets the bin folder where the tests are beeing executed.
        /// </summary>
        static public string BinFolder
        {
            get
            {
                if( _binFolder == null ) InitalizePaths();
                return _binFolder;
            }
        }

        /// <summary>
        /// Gets or sets the build configuration (Debug/Release).
        /// Default to the this CK.DB.Tests.NUnit configuration build.
        /// </summary>
        public static string BuildConfiguration
        {
            get
            {
                if( _solutionFolder == null )
                {
                    InitalizePaths();
                }
                return _buildConfiguration;
            }
        }

        public static string CurrentTestProjectName
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _currentTestProjectName;
            }
        }

        static void InitalizePaths()
        {
#if DEBUG
            _buildConfiguration = "Debug";
#else
            _buildConfiguration = "Release";
#endif
            string p = _binFolder = AppContext.BaseDirectory;
            string altConfDir = _buildConfiguration == "Release" ? "Debug" : "Release";
            string buildConfDir = FindAbove( p, _buildConfiguration ) ?? FindAbove( p, altConfDir );
            if( buildConfDir == null )
            {
                throw new InvalidOperationException( $"Unable to find parent folder named '{_buildConfiguration}' or '{altConfDir}' above '{_binFolder}'. Please explicitly set TestHelper.BuildConfiguration property." );
            }
            p = Path.GetDirectoryName( buildConfDir );
            if( Path.GetFileName( p ) != "bin" )
            {
                throw new InvalidOperationException( $"Folder '{_buildConfiguration}' MUST be in 'bin' folder (above '{_binFolder}')." );
            }
            _projectFolder = p = Path.GetDirectoryName( p );
            _currentTestProjectName = Path.GetFileName( p );
            Assembly entry = Assembly.GetEntryAssembly();
            if( entry != null )
            {
                string assemblyName = entry.GetName().Name;
                if( _currentTestProjectName != assemblyName )
                {
                    throw new InvalidOperationException( $"Current test project assembly is '{assemblyName}' but folder is '{_currentTestProjectName}' (above '{_buildConfiguration}' in '{_binFolder}')." );
                }
            }
            p = Path.GetDirectoryName( p );

            string testsFolder = null;
            bool hasGit = false;
            while( p != null && !(hasGit = Directory.Exists( Path.Combine( p, ".git" ) )) )
            {
                if( Path.GetFileName( p ) == "Tests" ) testsFolder = p;
                p = Path.GetDirectoryName( p );
            }
            if( !hasGit ) throw new InvalidOperationException( $"The project must be in a git repository (above '{_binFolder}')." );
            _repositoryFolder = p;
            if( testsFolder == null )
            {
                throw new InvalidOperationException( $"A parent 'Tests' folder must exist above '{_projectFolder}'." );
            }
            _solutionFolder = Path.GetDirectoryName( testsFolder );
            _logFolder = Path.Combine( testsFolder, "Logs" );
        }

        static string FindAbove( string path, string folderName )
        {
            while( path != null && Path.GetFileName( path ) != folderName )
            {
                path = Path.GetDirectoryName( path );
            }
            return path;
        }

    }
}
