using System.Collections.Generic;
using System.Linq;
using Paeezan.Utility;

namespace Paeezan.BSClient.Editor {
    public class BuildWizardProfiles {
        private static BuildWizardProfiles _ => __ ??= new BuildWizardProfiles();
        private static BuildWizardProfiles __;
        private List<SOBuildProfile> _profiles;
        
        public static List<SOBuildProfile> Profiles => _._profiles ??= RefreshProfileList();
        public static List<SOBuildProfile> RefreshProfileList() => _._profiles = ScriptableObjectTools.GetAssetsOfType<SOBuildProfile>().ToList();
    }
}