using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Indice.Common.Tests
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void CanFlattenCollectionWithNestedItems() {
            var menu = new List<MenuItem> {
                new MenuItem(1, new List<MenuItem> {
                    new MenuItem(2, new List<MenuItem> {
                        new MenuItem(3, new List<MenuItem> {
                            new MenuItem(4, null)
                        }), new MenuItem(5, null)
                    }), new MenuItem(6, null)
                })
            };
            var result = menu.SelectManyRecursive(x => x.Items);
            Assert.Equal(6, result.Count());
            Assert.Equal(3, result.Where(x => x.Name % 2 == 0).Count());
        }
        public record MenuItem(int Name, List<MenuItem> Items)
        {
            public override string ToString() => Name.ToString();
        }
    }
}
