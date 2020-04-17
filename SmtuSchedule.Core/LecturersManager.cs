using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public sealed class LecturersManager : ILecturersManager
    {
        public IReadOnlyDictionary<String, Int32>? LecturersMap
        {
            get
            {
                lock (_lecturersMapLock)
                {
                    return (_lecturersMap == null) ? null : new Dictionary<String, Int32>(_lecturersMap);
                }
            }
        }

        public Boolean IsLecturersMapReadedFromCache { get; private set; }

        public ILogger? Logger
        {
            get => _logger;
            set
            {
                _logger = value;
                _repository.Logger = value;
            }
        }

        public LecturersManager(String storagePath) : this()
        {
            if (String.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(storagePath));
            }

            _repository = new LocalLecturersRepository(storagePath);
        }

        internal LecturersManager(ILecturersRepository repository) : this()
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        private LecturersManager()
        {
            _repository = null!;
            _lecturersMapLock = new Object();
            _httpClient = new HttpClientProxy();
        }

        public Task<Boolean> ReadCachedLecturersMapAsync()
        {
            return Task.Run(() =>
            {
                IReadOnlyDictionary<String, Int32>? lecturersMap = _repository.ReadLecturersMap(
                    out Boolean hasNoReadingError);

                lock (_lecturersMapLock)
                {
                    if (lecturersMap != null)
                    {
                        _lecturersMap = lecturersMap;
                        IsLecturersMapReadedFromCache = true;
                    }
                }

                return hasNoReadingError;
            });
        }

        public Task<Boolean> DownloadLecturersMapAsync()
        {
            return DownloadLecturersMapAsync(new ServerLecturersDownloader(_httpClient) { Logger = _logger });
        }

        internal Task<Boolean> DownloadLecturersMapAsync(ILecturersDownloader downloader)
        {
            return Task.Run(async () =>
            {
                IReadOnlyDictionary<String, Int32>? lecturersMap = await downloader.DownloadLecturersMapAsync()
                    .ConfigureAwait(false);

                lock (_lecturersMapLock)
                {
                    if (lecturersMap != null)
                    {
                        _lecturersMap = lecturersMap;
                        IsLecturersMapReadedFromCache = false;
                        _repository.SaveLecturersMap(lecturersMap);
                    }
                }

                return downloader.HasNoDownloadingError;
            });
        }

        private readonly Object _lecturersMapLock;

        private ILogger? _logger;
        private readonly IHttpClient _httpClient;
        private readonly ILecturersRepository _repository;
        private IReadOnlyDictionary<String, Int32>? _lecturersMap;
    }
}