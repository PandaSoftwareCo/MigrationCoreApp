using MigrationCoreApp.Framework;
using System.Collections.ObjectModel;

namespace MigrationCoreApp.Models
{
    public class MigrationModel : BaseModel
    {
        public string MigrationId { get; set; }
        public string ContextKey { get; set; }
        public byte[] Model { get; set; }

        private string _content;
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

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

        private ObservableCollection<EdmxLine> _lines;
        public ObservableCollection<EdmxLine> Lines
        {
            get
            {
                return _lines;
            }
            set
            {
                if (_lines != value)
                {
                    _lines = value;
                    OnPropertyChanged(nameof(Lines));
                }
            }
        }

        public MigrationModel()
        {
            Lines = new ObservableCollection<EdmxLine>();
        }
    }
}
