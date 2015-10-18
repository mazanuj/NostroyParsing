using System.Collections.Generic;
using NostroyParsingLib.DataTypes;

namespace NostroyParsingLib
{
    public static class QueueHelper
    {
        static QueueHelper()
        {
            ParsingQueue = new Queue<string>();
            Collection = new MainCollection();
            PageError = new List<string>();
            RowError = new List<string>();
        }

        internal static List<string> PageError { get; set; }
        internal static List<string> RowError { get; set; }
        private static MainCollection Collection { get; set; }
        private static Queue<string> ParsingQueue { get; }

        public static List<MainType> CollectionSaver
        {
            set { Collection.MainTypeList.AddRange(value); }
            get { return Collection.MainTypeList; }
        }

        internal static List<string> QueueSaver
        {
            set
            {
                var list = value;
                foreach (var val in list)
                    ParsingQueue.Enqueue(val);
            }
            get
            {
                var list = new List<string>();
                while (ParsingQueue.Count > 0)
                    list.Add(ParsingQueue.Dequeue());
                return list;
            }
        }
    }
}