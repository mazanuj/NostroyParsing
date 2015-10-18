using System.Collections.Generic;

namespace NostroyParsingLib.DataTypes
{
    public class MainType
    {
        public string SRO { get; set; }
        public string ShortName { get; set; }
        public string INN { get; set; }
        public string Phone { get; set; }
        public string FIO { get; set; }

        public Status Status { get; set; }
    }

    public class MainCollection
    {
        public MainCollection()
        {
            MainTypeList = new List<MainType>();
        }

        public List<MainType> MainTypeList { get; set; }
    }

    public enum Status
    {
        Member,
        Excude
    }
}