using System;
using System.Threading.Tasks;
using Xunit;

namespace Pomelo.Wow.EventRegistration.WCL.Tests
{
    public class FetcherTests
    {
        [Fact]
        public async Task FetchCharacterTest()
        {
            var ch = await Fetcher.FetchAsync("√»–°Ë÷", "¡˙÷Æ’ŸªΩ", Models.CharactorRole.Tank, 3);

            Assert.NotNull(ch);
        }
    }
}
