﻿#if SYNC_CLIENT
using NUnit.Framework;
using System.Linq;
using NSubstitute;
using Octopus.Client.Extensions;

namespace Octopus.Client.Tests.Repositories
{
    public class OctopusRepositoryTests
    {
        [Test]
        public void AllPropertiesAreNotNull()
        {
            var repository = new OctopusRepository(Substitute.For<IOctopusClient>());
            var nullPropertiesQ = from p in typeof(OctopusRepository).GetProperties()
                where p.GetMethod.Invoke(repository, new object[0]) == null
                select p.Name;

            var nullProperties = nullPropertiesQ.ToArray();
            if (nullProperties.Any())
                Assert.Fail("The following properties are null after OctopusAsyncRepository instantiation: " + nullProperties.CommaSeperate());
        }
    }
}
#endif