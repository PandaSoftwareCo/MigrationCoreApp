using MigrationCoreApp.Framework;

namespace MigrationCoreApp.Models
{
    public class EdmxLine : BaseModel
    {
        public int LineNumber { get; set; }
        //private int _lineNumber;
        //public int LineNumber
        //{
        //    get
        //    {
        //        return _lineNumber;
        //    }
        //    set
        //    {
        //        if (_lineNumber != value)
        //        {
        //            _lineNumber = value;
        //            OnPropertyChanged(nameof(LineNumber));
        //        }
        //    }
        //}

        public string LineContent { get; set; }
        //private string _lineContent;
        //public string LineContent
        //{
        //    get
        //    {
        //        return _lineContent;
        //    }
        //    set
        //    {
        //        if (_lineContent != value)
        //        {
        //            _lineContent = value;
        //            OnPropertyChanged(nameof(LineContent));
        //        }
        //    }
        //}

        public string ProductVersion { get; set; }

        private bool _changed;
        public bool Changed
        {
            get
            {
                return _changed;
            }
            set
            {
                if (_changed != value)
                {
                    _changed = value;
                    OnPropertyChanged(nameof(Changed));
                }
            }
        }

        private string _databaseName;
        public string DatebaseName
        {
            get
            {
                return _databaseName;
            }
            set
            {
                if (_databaseName != value)
                {
                    _databaseName = value;
                    OnPropertyChanged(nameof(DatebaseName));
                }
            }
        }
    }
}
