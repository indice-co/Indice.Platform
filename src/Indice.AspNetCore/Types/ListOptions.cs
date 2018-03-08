using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Types
{
    /// <summary>
    /// List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.
    /// </summary>
    public class ListOptions
    {
        /// <summary>
        /// Object Representation for a sort by clause
        /// </summary>
        public class Sorting
        {
            const string ASC = nameof(ASC);
            const string DESC = nameof(DESC);

            /// <summary>
            /// the property path
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// the sort direction (ASC, DESC)
            /// </summary>
            public string Direction { get; set; }


            /// <summary>
            /// Gets the string representation. ex Name+
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"{Path} ({Direction})";

            /// <summary>
            /// Moves the current state of this sort clause to the next state.
            /// Say if we had sort by "Name ASC" it cycles between ASC DESC and none each time it is called.
            /// It returns a new <see cref="Sorting"/> and does not change the current reference.
            /// </summary>
            /// <returns></returns>
            public Sorting NextState() {
                switch (Direction) {
                    case ASC:
                        return new Sorting { Direction = DESC, Path = Path };
                    case DESC:
                        return null;
                    default:
                        throw new Exception($"unexpected direction {Direction}");
                }
            }

            /// <summary>
            /// Parse the string representation  for a sort path with direction into a <see cref="Sorting"/>
            /// </summary>
            /// <param name="text">a property path (case agnostic) followed by a sing '+' or '-'.</param>
            /// <returns></returns>
            public static Sorting Parse(string text) {
                if (string.IsNullOrWhiteSpace(text)) {
                    throw new ArgumentOutOfRangeException(nameof(text));
                }

                var raw = text.Trim();

                var sort = new Sorting {
                    Path = raw.TrimEnd('-', '+'),
                    Direction = raw.EndsWith("-", StringComparison.OrdinalIgnoreCase) ? DESC : ASC
                };

                return sort;
            }
        }

        /// <summary>
        /// Creates instance with default parameters
        /// </summary>
        public ListOptions() {
            Page = 1;
            Size = 100;
            Sort = string.Empty;
        }

        /// <summary>
        /// The current page of the list. Default is 1.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// The size of the list. Default is 100.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// The property name used to sort the list.
        /// </summary>
        public string Sort { get; set; }

        /// <summary>
        /// A search term used to limit the results of the list.
        /// </summary>
        public string Search { get; set; }

        /// <summary>
        /// Retrieves the number of pages for a total of <see cref="count"/> results.
        /// </summary>
        /// <param name="count">The number of results</param>
        /// <returns></returns>
        public int GetPagesFor(int count) => (int)Math.Ceiling(count / (double)Size);

        /// <summary>
        /// Break the Sort parameter into multiple sort by clauses. 
        /// Takes the Name-,Date+ etc... and enumerates it.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Sorting> GetSortings() {
            var list = (Sort ?? string.Empty).Split(',');

            foreach (var item in list) {
                if (string.IsNullOrWhiteSpace(item)) {
                    continue;
                }

                yield return Sorting.Parse(item);
            }
        }

        /// <summary>
        /// Convert all the list parameters into
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<string, object> ToDictionary() {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { nameof(Page).ToLower(), Page.ToString() },
                { nameof(Size).ToLower(), Size.ToString() },
            };

            if (!string.IsNullOrWhiteSpace(Sort)) {
                dictionary.Add(nameof(Sort).ToLower(), Sort);
            }

            if (!string.IsNullOrWhiteSpace(Search)) {
                dictionary.Add(nameof(Search).ToLower(), Search);
            }

            return dictionary;
        }
    }

    /// <summary>
    /// A variant of list options that allows for the use of a custom filter model. 
    /// More advanced than plain a search text.
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    public class ListOptions<TFilter> : ListOptions where TFilter : class, new()
    {
        public ListOptions() : base() => Filter = new TFilter();

        public TFilter Filter { get; set; }

        public override IDictionary<string, object> ToDictionary() {
            var dictionary = base.ToDictionary();
            return dictionary.Merge(Filter, typeof(TFilter), "filter.");
        }
    }
}
