using System.Collections;

namespace ASPNetCoreGraphQlServer.Models
{
    public class Group<T> : List<Group<T>>
    {
        public string GroupGuid { get; set; }

        public int Level { get; set; }

        public int ChildLevels { get; set; }

        public object[] Records { get; set; }

        public object Key { get; set; }

        public int CountItems { get; set; }

        public IEnumerable Items { get; set; }

        public object Aggregates { get; set; }

        public string Field { get; set; }

        public string HeaderText { get; set; }

        public string ForeignKey { get; set; }

        public object Result { get; set; }

        internal IEnumerable GroupedData { get; set; }
    }
}
