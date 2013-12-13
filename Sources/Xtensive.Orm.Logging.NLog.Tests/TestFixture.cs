﻿using System;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using NLogManager = NLog.LogManager;

namespace Xtensive.Orm.Logging.NLog.Tests
{
    [TestFixture]
    public class TestFixture
    {
        [Test]
        public void LogManagerTest()
        {
            var domainConfiguration = DomainConfiguration.Load("Default");
            Domain.Build(domainConfiguration);

            var logger = LogManager.Default.GetLog("Xtensive.Orm");
            Assert.IsInstanceOf<Orm.Logging.NLog.Log>(logger);
        }
    }
}