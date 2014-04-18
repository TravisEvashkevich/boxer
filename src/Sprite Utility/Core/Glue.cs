using Boxer.Data;

namespace Boxer.Core
{
    public class Glue : MainViewModel
    {
        private static Glue _instance;
        public static Glue Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Glue();
                return _instance;
            }
        }

        private bool _documentIsSaved;
        public bool DocumentIsSaved
        {
            get { return _documentIsSaved; }
            set
            {
                Set(ref _documentIsSaved, value);
            }
        }

        private Document _document;

        public Document Document
        {
            get { return _document; }
            set { Set(ref _document, value); }
        }

        protected override void InitializeCommands()
        {
        }
    }
}
