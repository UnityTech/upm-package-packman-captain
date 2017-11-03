using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.PackageManager.UI.Tests;
using UnityEngine.Experimental.UIElements.StyleSheets;
using Toggle = UnityEngine.Experimental.UIElements.Toggle;

namespace UnityEditor.PackageManager.UI.Captain
{
    internal class CaptainWindow : EditorWindow
    {
        private VisualElement root;
        
        // When object is created
        public void OnEnable()
        {
            this.GetRootVisualContainer().AddStyleSheetPath("Styles/CaptainStyles");
            root = Resources.Load<VisualTreeAsset>("Templates/CaptainWindow").CloneTree(null);
            this.GetRootVisualContainer().Add(root);
            root.StretchToParentSize();
            this.GetRootVisualContainer().StretchToParentSize();
            
            // TODO: Using Label instead of Button because Button in uxml already
            //              have a manipulator that cannot be overridden. (pending issue)
            foreach (var setname in new string[] {"empty", "add", "many", "test-states", "outdated", "real"})
                root.Q<Label>(setname).AddManipulator(new Clickable(() => SetPackageSets(setname)));
            
            root.Q<Label>("refresh").AddManipulator(new Clickable(FullRefresh));
            
            root.Q<TextField>("speed").RegisterCallback<InputEvent>(RequestSpeedChanged);
            
            root.Q<Label>("update").AddManipulator(new Clickable(ForceUpdateRequestError));
        }

        private void FullRefresh()
        {
            SetActiveMode(null);
            PackageCollection.Instance.Reset();
        }

        private Action<UpmBaseOperation> speedHandler;
        private void RequestSpeedChanged(InputEvent input)
        {            
            int value = 0;
            int.TryParse(input.newData, out value);

            speedHandler = operation =>
            {
                var delay =     Utilities.IsSameOrSubclass<UpmAddOperation>(operation) && root.Q<Toggle>("toggleAdd").on || 
                                    Utilities.IsSameOrSubclass<UpmListOperation>(operation) && root.Q<Toggle>("toggleList").on ||
                                    Utilities.IsSameOrSubclass<UpmSearchOperation>(operation) && root.Q<Toggle>("toggleSearch").on; 

                if (delay)
                    operation.Delay.Length = value;
            };

            UpmBaseOperation.OnOperationStart -= speedHandler;
            UpmBaseOperation.OnOperationStart += speedHandler;
        }

        private void SetPackageSets(string type)
        {
            if (type == "empty")
                PackageCollection.Instance.ClearPackages();
            else if (type == "add")
                PackageCollection.Instance.AddPackageInfo(PackageSets.Instance.Single());
            else if (type == "many")
                PackageCollection.Instance.AddPackageInfos(PackageSets.Instance.Many(100));
            else if (type == "test-states")
                AddTestPackages();
            else if (type == "outdated")
                PackageCollection.Instance.AddPackageInfos(PackageSets.Instance.Outdated());
            else if (type == "real")
                PackageCollection.Instance.SetPackageInfos(PackageSets.Instance.RealPackages());
        }

        private PackageManagerWindow Slave
        {
            get {return GetWindow<PackageManagerWindow>();}
        }

        private bool isForcingError;
        private void ForceUpdateRequestError()
        {
            if (isForcingError)
            {
                root.Q<Label>("update").style.backgroundColor = new StyleValue<Color>();
                UpmBaseOperation.OnOperationStart -= OnUpmBaseOperationOnOnOperationStart;
            }
            else
            {
                root.Q<Label>("update").style.backgroundColor = new StyleValue<Color>(new Color(0.2f, 0.5f, 0.2f));
                UpmBaseOperation.OnOperationStart += OnUpmBaseOperationOnOnOperationStart;                
            }

            isForcingError = !isForcingError;
        }

        private void OnUpmBaseOperationOnOnOperationStart(UpmBaseOperation operation)
        {
            if (operation != null)
                operation.ForceError = JsonUtility.FromJson<Error>("{\"errorCode\" : 0, \"message\" : \"Captain window forced error!\"}");
        }

        private void AddTestPackages()
        {
            // Let's setup our test factory
            var factory = new MockOperationFactory();
            factory.Packages = PackageSets.Instance.TestData();
          
            SetActiveMode(factory);

            PackageCollection.Instance.RefreshPackages();
        }

        private void SetActiveMode(IOperationFactory operationFactory)
        {
            if (operationFactory != null)
            {
                // Assign the fake operation factory
                OperationFactory.Instance = operationFactory;
                MainContainer.AddToClassList("activeBorder");
                MainContainer.RemoveFromClassList("inactiveBorder");                
            }
            else
            {
                OperationFactory.Reset();
                MainContainer.RemoveFromClassList("activeBorder");                
                MainContainer.AddToClassList("inactiveBorder");                
            }
        }

        [MenuItem("internal:Project/Packages/[Captain]")]
        public static void ShowPackageManagerCaptain()
        {
            var window = GetWindow<CaptainWindow>(true, "Package Manager Captain", true);
            window.minSize = new Vector2(400, 450);
            window.maxSize = new Vector2(1200, 800);
            window.Show();
        }

        private VisualContainer MainContainer { get { return root.Q<VisualContainer>("main"); }}
    }
}
