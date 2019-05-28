using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Collection of <see cref="string"/> that represent a scope name. This class differs from any other
    /// collection of strings in that it checks the string to see if it represents a pattern. 
    /// It implements a custom version of <see cref="ICollection{T}.Contains(T)"/> that will do regex match if an internal item is Identified as a regex.
    /// </summary>
    public class DynamicScopeNameCollection : ICollection<string>, IEnumerable
    {
        /// <summary>
        /// The source collection
        /// </summary>
        protected ICollection<string> Source { get; }

        /// <summary>
        /// constructs the collection
        /// </summary>
        /// <param name="source"></param>
        public DynamicScopeNameCollection(IEnumerable<string> source) {
            if (source is ICollection<string>) {
                Source = source as ICollection<string>;
            } else {
                Source = new List<string>(source ?? new string[0]);
            }
        }

        /// <summary>
        /// Gets the number of elements
        /// </summary>
        public int Count => Source.Count;

        /// <summary>
        /// Gets a value indicating if the collection is read only
        /// </summary>
        public bool IsReadOnly => Source.IsReadOnly;

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <param name="item"></param>
        public void Add(string item) => Source.Add(item);

        /// <summary>
        /// Removes all items from the collection
        /// </summary>
        public void Clear() => Source.Clear();

        /// <summary>
        /// Checks the incoming name if it is exactily contained in the collection.
        /// If not then it checks against any of the items that can be interpreted as Regex patterns for matches. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) {
            var success = Source.Where(x => x == name).Any();
            if (!success) {
                var matches = Source.Where(x => x.IsPattern() && new Regex(x).IsMatch(name));
                success = matches.Any();
            }
            return success;
        }

        /// <summary>
        /// Copies the elements of the collection
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(string[] array, int arrayIndex) => Source.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes an element from the collection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name) => Source.Remove(name);

        /// <summary>
        /// Returns an enumerator that iterates the items of the collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator() => Source.GetEnumerator();


        /// <summary>
        /// Returns an enumerator that iterates the items of the collection
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}