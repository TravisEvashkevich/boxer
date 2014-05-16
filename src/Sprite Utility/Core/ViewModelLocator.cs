/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:Boxer"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using Boxer.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Core
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainWindowVM>();
            SimpleIoc.Default.Register<PreferencesVM>();
            SimpleIoc.Default.Register<DocumentViewVM>();
            SimpleIoc.Default.Register<FolderViewVM>();
            SimpleIoc.Default.Register<ImageFrameViewVM>();
            SimpleIoc.Default.Register<ImageViewVM>();
            SimpleIoc.Default.Register<AutoTraceWindowVM>();
            SimpleIoc.Default.Register<SearchFilterVM>();
            SimpleIoc.Default.Register<MergeVM>();
            SimpleIoc.Default.Register<Glue>();
        }

        public MainWindowVM MainWindow
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.Initialize();
                return instance;
            }
        }

        public PreferencesVM Preferences
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<PreferencesVM>();
                instance.Initialize();
                return instance;
            }
        }

        public MergeVM MergeVm
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<MergeVM>();
                instance.Initialize();
                return instance;
            }
        }

        public SearchFilterVM SearchFilter
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<SearchFilterVM>();
                instance.Initialize();
                return instance;
                
            }
        }

        public DocumentViewVM DocumentView
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<DocumentViewVM>();
                instance.Initialize();
                return instance;
            }
        }

        public FolderViewVM FolderView
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<FolderViewVM>();
                instance.Initialize();
                return instance;
            }
        }

        public ImageFrameViewVM ImageFrameView
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<ImageFrameViewVM>();
                instance.Initialize();
                return instance;
            }
        }

        public ImageViewVM ImageView
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<ImageViewVM>();
                instance.Initialize();
                return instance;
            }
        }

        public AutoTraceWindowVM AutoTraceWindow
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<AutoTraceWindowVM>();
                instance.Initialize();
                return instance;
            }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}