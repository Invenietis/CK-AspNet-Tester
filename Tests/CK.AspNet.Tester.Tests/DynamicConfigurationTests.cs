using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.AspNet.Tester.Tests
{
    [TestFixture]
    public class DynamicConfigurationTests
    {
        class ConfigWatcher
        {
            readonly IConfigurationRoot _root;
            bool _hasChanged;

            public ConfigWatcher( IConfigurationRoot root )
            {
                _root = root;
                Watch();
            }

            public void CheckChange() => DoCheckChange( null, null );

            public void CheckChange( string key, string expectedValue ) => DoCheckChange( key ?? throw new ArgumentNullException( nameof( key ) ), expectedValue );

            void DoCheckChange( string key, string expectedValue )
            {
                _hasChanged.Should().BeTrue();
                _hasChanged = false;
                if( key != null )
                {
                    _root[key].Should().Be( expectedValue, $"Expected '{expectedValue}' for key '{key}'." );
                }
                Watch();
            }

            void Watch()
            {
                IChangeToken t = _root.GetReloadToken();
                t.ActiveChangeCallbacks.Should().BeTrue();
                t.RegisterChangeCallback( SetChange, null );
            }

            void SetChange( object _ ) => _hasChanged = true;

        }

        [Test]
        public void DynamicConfigurationSource_works()
        {
            var conf = new DynamicConfigurationSource();

            var builder = new ConfigurationBuilder();
            builder.Add( conf );
            IConfigurationRoot result = builder.Build();

            var watcher = new ConfigWatcher( result );

            conf["A"] = "B";
            watcher.CheckChange( "A", "B" );

            conf["A"] = "p";
            watcher.CheckChange( "A", "p" );

            conf["A"] = null;
            watcher.CheckChange( "A", null );

            conf.Remove( "A" );
            watcher.CheckChange( "A", null );
        }

        [Test]
        public void using_multiple_DynamicConfigurationSource()
        {
            var conf = new DynamicConfigurationSource();
            var confWin = new DynamicConfigurationSource();

            var builder = new ConfigurationBuilder();
            builder.Add( conf );
            builder.Add( confWin );
            IConfigurationRoot result = builder.Build();

            var watcher = new ConfigWatcher( result );

            result["A"].Should().Be( null );
            conf["A"] = "Low";
            watcher.CheckChange( "A", "Low" );

            confWin["A"] = "High";
            watcher.CheckChange( "A", "High" );

            conf["A"] = "NOT VISIBLE";
            watcher.CheckChange( "A", "High" );

            conf["A"] = "Low";
            watcher.CheckChange( "A", "High" );

            confWin["A"] = null;
            watcher.CheckChange( "A", null );

            confWin.Remove( "A" );
            watcher.CheckChange( "A", "Low" );

            conf.Remove( "A" );
            watcher.CheckChange( "A", null );

        }

        [Test]
        public void DynamicJsonConfigurationSource_works()
        {
            var conf = new DynamicJsonConfigurationSource( @"{}" );

            var builder = new ConfigurationBuilder();
            builder.Add( conf );
            IConfigurationRoot result = builder.Build();

            var watcher = new ConfigWatcher( result );

            conf.SetJson( @"{ ""A"": ""B"" }" );
            watcher.CheckChange( "A", "B" );

            conf.SetJson( @"{ ""A"": null }" );
            watcher.CheckChange( "A", "" );

            conf.SetJson( @"{ ""A"": true }" );
            watcher.CheckChange( "A", "True" );

        }
    }
}
