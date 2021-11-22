using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MigrationCoreApp.Framework;
using MigrationCoreApp.Interfaces;
using MigrationCoreApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace MigrationCoreApp.ViewModels
{
    public class MainWindowViewModel : BaseModel
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        public IAppSettings Settings { get; set; }
        private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
        protected internal virtual IDisposable ChangeListener { get; }

        private ObservableCollection<MigrationModel> _migrations = new ObservableCollection<MigrationModel>();
        public ObservableCollection<MigrationModel> Migrations
        {
            get
            {
                return _migrations;
            }
            set
            {
                if (_migrations != value)
                {
                    _migrations = value;
                    OnPropertyChanged(nameof(Migrations));
                }
            }
        }

        private MigrationModel _selectedMigration;
        public MigrationModel SelectedMigration
        {
            get
            {
                return _selectedMigration;
            }
            set
            {
                if (_selectedMigration != value)
                {
                    _selectedMigration = value;
                    if (_selectedMigration != null && string.IsNullOrWhiteSpace(_selectedMigration?.Content))
                    {
                        Task.Run(async () =>
                        {
                            await LoadMigration(Settings.DefaultConnection, Migrations, SelectedMigration);
                        });
                    }

                    OnPropertyChanged(nameof(SelectedMigration));
                }
            }
        }

        private ObservableCollection<MigrationModel> _secondaryMigrations = new ObservableCollection<MigrationModel>();
        public ObservableCollection<MigrationModel> SecondaryMigrations
        {
            get
            {
                return _secondaryMigrations;
            }
            set
            {
                if (_secondaryMigrations != value)
                {
                    _secondaryMigrations = value;
                    OnPropertyChanged(nameof(SecondaryMigrations));
                }
            }
        }

        private MigrationModel _secondarySelectedMigration;
        public MigrationModel SecondarySelectedMigration
        {
            get
            {
                return _secondarySelectedMigration;
            }
            set
            {
                if (_secondarySelectedMigration != value)
                {
                    _secondarySelectedMigration = value;
                    if (_secondarySelectedMigration != null && string.IsNullOrWhiteSpace(_secondarySelectedMigration?.Content))
                    {
                        Task.Run(async () =>
                        {
                            await LoadMigration(Settings.SecondaryConnection, SecondaryMigrations, SecondarySelectedMigration);
                        });
                    }

                    OnPropertyChanged(nameof(SecondarySelectedMigration));
                }
            }
        }

        private ObservableCollection<EdmxLine> _primaryChanges = new ObservableCollection<EdmxLine>();
        public ObservableCollection<EdmxLine> PrimaryChanges
        {
            get
            {
                return _primaryChanges;
            }
            set
            {
                if (_primaryChanges != value)
                {
                    _primaryChanges = value;
                    OnPropertyChanged(nameof(PrimaryChanges));
                }
            }
        }

        private ObservableCollection<EdmxLine> _secondaryChanges = new ObservableCollection<EdmxLine>();
        public ObservableCollection<EdmxLine> SecondaryChanges
        {
            get
            {
                return _secondaryChanges;
            }
            set
            {
                if (_secondaryChanges != value)
                {
                    _secondaryChanges = value;
                    OnPropertyChanged(nameof(SecondaryChanges));
                }
            }
        }

        private string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public MainWindowViewModel(IAppSettings settings, ILogger<MainWindowViewModel> logger, ILoggerFactory loggerFactory, IOptionsMonitor<AppSettings> optionsMonitor)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings)); ;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            ChangeListener = _optionsMonitor.OnChange(OnMyOptionsChange);
            Status = "Ready";
        }

        private void OnMyOptionsChange(AppSettings settings, string name)
        {
            Settings = settings;
        }

        private AsyncCommand _loadAsyncCommand;
        public AsyncCommand LoadAsyncCommand
        {
            get
            {
                if (_loadAsyncCommand == null)
                {
                    _loadAsyncCommand = new AsyncCommand(async execute => await LoadAsync(), canExecute => CanLoad());
                }
                return _loadAsyncCommand;
            }
            set
            {
                _loadAsyncCommand = value;
            }
        }

        public async Task LoadAsync()
        {
            var window = System.Windows.Application.Current.MainWindow;
            window.Cursor = System.Windows.Input.Cursors.Wait;
            var watchForParallel = Stopwatch.StartNew();

            var task1 = LoadData(Settings.DefaultConnection, Migrations, SelectedMigration);
            var task2 = LoadData(Settings.SecondaryConnection, SecondaryMigrations, null);
            var tasks = new List<Task> {task1, task2};
            await Task.WhenAll(tasks);
            var primaryConnection = new SqlConnection(Settings.DefaultConnection);
            var secondaryConnection = new SqlConnection(Settings.SecondaryConnection);
            foreach (var item in Migrations)
            {
                item.DatebaseName = primaryConnection.Database;
                if (SecondaryMigrations.SingleOrDefault(i => i.MigrationId == item.MigrationId) == null)
                {
                    item.Changed = true;
                    _logger.LogInformation($"{item.DatebaseName} - {item.MigrationId}");
                }
            }
            foreach (var item in SecondaryMigrations)
            {
                item.DatebaseName = secondaryConnection.Database;
                if (Migrations.SingleOrDefault(i => i.MigrationId == item.MigrationId) == null)
                {
                    item.Changed = true;
                    _logger.LogInformation($"{item.DatebaseName} - {item.MigrationId}");
                }
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"LOAD: ELAPSED: {time} {primaryConnection.Database} COUNT: {Migrations.Count} {secondaryConnection.Database} COUNT: {SecondaryMigrations.Count}";
            _logger.LogInformation(Status);
            window.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private bool CanLoad()
        {
            return true;
        }

        private AsyncCommand _compareAsyncCommand;

        public AsyncCommand CompareAsyncCommand
        {
            get
            {
                if (_compareAsyncCommand == null)
                {
                    _compareAsyncCommand = new AsyncCommand(async execute => await CompareAsync(), canExecute => CanCompare());
                }
                return _compareAsyncCommand;
            }
            set
            {
                _compareAsyncCommand = value;
            }
        }

        public async Task CompareAsync()
        {
            var window = System.Windows.Application.Current.MainWindow;
            window.Cursor = System.Windows.Input.Cursors.Wait;
            var watchForParallel = Stopwatch.StartNew();

            int i = 0, j = 0;
            while (i < SelectedMigration.Lines.Count && j < SecondarySelectedMigration.Lines.Count)
            {
                if (SelectedMigration.Lines[i].LineContent == SecondarySelectedMigration.Lines[j].LineContent)
                {
                    if (i < SelectedMigration.Lines.Count) i++;
                    if (j < SecondarySelectedMigration.Lines.Count) j++;
                }
                else
                {
                    var primaryArray = SelectedMigration.Lines[i].LineContent.ToCharArray();
                    var secondaryArray = SecondarySelectedMigration.Lines[j].LineContent.ToCharArray();

                    int i2 = i + 1, j2 = j + 1;
                    while (i < SelectedMigration.Lines.Count && j < SecondarySelectedMigration.Lines.Count)
                    {
                        if (SelectedMigration.Lines[i].LineContent == SecondarySelectedMigration.Lines[j2].LineContent)
                        {
                            for (int k = j; k < j2; k++)
                            {
                                _logger.LogInformation($"{k} {SecondarySelectedMigration.Lines[k].DatebaseName} {SecondarySelectedMigration.Lines[k].LineContent}");
                                SecondarySelectedMigration.Lines[k].Changed = true;
                            }
                            j = j2;
                            break;
                        }
                        if (SecondarySelectedMigration.Lines[j].LineContent == SelectedMigration.Lines[i2].LineContent)
                        {
                            for (int k = i; k < i2; k++)
                            {
                                _logger.LogInformation($"{k} {SelectedMigration.Lines[k].DatebaseName} {SelectedMigration.Lines[k].LineContent}");
                                SelectedMigration.Lines[k].Changed = true;
                            }
                            i = i2;
                            break;
                        }
                        if (SecondarySelectedMigration.Lines[j2].LineContent == SelectedMigration.Lines[i2].LineContent)
                        {
                            for (int k = i; k < i2; k++)
                            {
                                _logger.LogInformation($"{k} {SelectedMigration.Lines[k].DatebaseName} {SelectedMigration.Lines[k].LineContent}");
                                SelectedMigration.Lines[k].Changed = true;
                            }
                            for (int k = j; k < j2; k++)
                            {
                                _logger.LogInformation($"{k} {SecondarySelectedMigration.Lines[k].DatebaseName} {SecondarySelectedMigration.Lines[k].LineContent}");
                                SecondarySelectedMigration.Lines[k].Changed = true;
                            }
                            i = i2;
                            j = j2;
                            break;
                        }

                        if (i2 < SelectedMigration.Lines.Count) i2++;
                        if (j2 < SecondarySelectedMigration.Lines.Count) j2++;
                    }
                    if (i < SelectedMigration.Lines.Count) i++;
                    if (j < SecondarySelectedMigration.Lines.Count) j++;
                }
            }

            PrimaryChanges.Clear();
            foreach (var edmxLine in SelectedMigration.Lines.Where(i => i.Changed))
            {
                PrimaryChanges.Add(edmxLine);
            }
            SecondaryChanges.Clear();
            foreach (var edmxLine in SecondarySelectedMigration.Lines.Where(i => i.Changed))
            {
                SecondaryChanges.Add(edmxLine);
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"COMPARE: ELAPSED: {time}";
            _logger.LogInformation(Status);
            window.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private bool CanCompare()
        {
            return SelectedMigration != null && SecondarySelectedMigration != null;
        }

        private async Task LoadData(string connectionString, ObservableCollection<MigrationModel> collection,
            MigrationModel selected)
        {
            collection.Clear();
            var connection = new SqlConnection(connectionString);
            var query = await connection.QueryAsync<MigrationModel>(@"SELECT [MigrationId]
      ,[ContextKey]
--      ,[Model]
      ,[ProductVersion]
  FROM [dbo].[__MigrationHistory]");
            var history = query.ToList();
            foreach (var item in history)
            {
                collection.Add(item);
            }
        }

        private async Task LoadMigration(string connectionString, ObservableCollection<MigrationModel> collection,
            MigrationModel selectedMigration)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = System.Windows.Application.Current.MainWindow;
                window.Cursor = System.Windows.Input.Cursors.Wait;
            });

            var p = new
            {
                MigrationId = selectedMigration.MigrationId
            };

            SqlConnection connection = new SqlConnection(connectionString);
            var data = await connection.QueryAsync<EdmxModel>($@"declare @binaryContent varbinary(max);
 
select @binaryContent = Model
    from [dbo].[__MigrationHistory]
where migrationId = @MigrationId
 
select cast('' as xml).value('xs:base64Binary(sql:variable(""@binaryContent""))',
            'varchar(max)') as base64Content", p);
            foreach (var item in data)
            {
                string edmx = Decompress(item.Base64Content);
                selectedMigration.Content = edmx;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    selectedMigration.Lines.Clear();
                });
                var reader = new StringReader(edmx);
                int count = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    count++;
                    Debug.WriteLine("Line {0}: {1}", count, line);
                    EdmxLine l = new EdmxLine
                    {
                        LineNumber = count,
                        LineContent = line,//.Trim()
                        DatebaseName = selectedMigration.DatebaseName
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        selectedMigration.Lines.Add(l);
                    });
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = System.Windows.Application.Current.MainWindow;
                window.Cursor = System.Windows.Input.Cursors.Arrow;
            });
        }

        private string Decompress(string content)
        {
            byte[] bytes = Convert.FromBase64String(content);
            using (GZipStream stream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];

                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return Encoding.UTF8.GetString(memory.ToArray());
                }
            }
        }

        private AsyncCommand _saveAsyncCommand;
        public AsyncCommand SaveAsyncCommand
        {
            get
            {
                if (_saveAsyncCommand == null)
                {
                    _saveAsyncCommand = new AsyncCommand(async execute => await SaveAsync(), canExecute => CanSave());
                }
                return _saveAsyncCommand;
            }
            set
            {
                _saveAsyncCommand = value;
            }
        }

        public async Task SaveAsync()
        {
            var window = System.Windows.Application.Current.MainWindow;
            window.Cursor = System.Windows.Input.Cursors.Wait;
            var watchForParallel = Stopwatch.StartNew();

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.FileName = "Document"; // Default file name
            saveFileDialog.DefaultExt = ".txt"; // Default file extension
            saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            if (saveFileDialog.ShowDialog() == true)
            {
                string filename = saveFileDialog.FileName;
                var stringBuilder = new StringBuilder();
                foreach (var primaryChange in PrimaryChanges)
                {
                    stringBuilder.AppendLine(
                        $"{primaryChange.LineNumber} {primaryChange.DatebaseName} {primaryChange.LineContent}");
                }

                foreach (var secondaryChange in SecondaryChanges)
                {
                    stringBuilder.AppendLine(
                        $"{secondaryChange.LineNumber} {secondaryChange.DatebaseName} {secondaryChange.LineContent}");
                }

                await File.WriteAllTextAsync(saveFileDialog.FileName, stringBuilder.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"SAVE: ELAPSED: {time}";
            _logger.LogInformation(Status);
            window.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private bool CanSave()
        {
            return PrimaryChanges.Any() || SecondaryChanges.Any();
        }
    }
}
