#if UNITY_EDITOR && (UNITY_STANDALONE_OSX || CT_DEVELOP)
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using Crosstales.FB.EditorUtil;

namespace Crosstales.FB.EditorBuild
{
   /// <summary>Post processor for macOS.</summary>
   public static class MacOSPostProcessor
   {
      private const string ID = "com.crosstales.fb";

      [PostProcessBuildAttribute(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (EditorHelper.isMacOSPlatform)
         {
            //remove all meta-files
            try
            {
               string[] files = Crosstales.Common.Util.FileHelper.GetFiles(pathToBuiltProject, true, "meta");

               foreach (string file in files)
               {
                  try
                  {
                     Crosstales.Common.Util.FileHelper.DeleteFile(file);
                  }
                  catch (System.Exception e)
                  {
                     Debug.LogWarning($"Could not delete file: {file} - {e}");
                  }
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not delete files: {ex}");
            }

            //rewrite Info.plist
            if (EditorConfig.MACOS_MODIFY_BUNDLE)
            {
               try
               {
                  string[] files = Crosstales.Common.Util.FileHelper.GetFiles(pathToBuiltProject, true, "plist");

                  foreach (string file in files)
                  {
                     try
                     {
                        string content = Crosstales.Common.Util.FileHelper.ReadAllText(file);

                        if (content.Contains(ID) && !content.Contains($"{ID}."))
                        {
                           content = content.Replace(ID, $"{ID}.{System.DateTime.Now:yyyyMMddHHmmss}");
                           Crosstales.Common.Util.FileHelper.WriteAllText(file, content);
                        }
                     }
                     catch (System.Exception e)
                     {
                        Debug.LogWarning($"Could not read/write 'Info.plist' file: {file} - {e}");
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning($"Could not scan for 'Info.plist' files: {ex}");
               }
               //UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle("/path/to/bundle.bundle"); //TODO add for Unity > 2018?
            }
         }
      }
   }
}
#endif
// © 2017-2024 crosstales LLC (https://www.crosstales.com)