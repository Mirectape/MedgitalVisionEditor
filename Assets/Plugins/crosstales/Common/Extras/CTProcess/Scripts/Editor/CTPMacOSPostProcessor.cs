#if UNITY_EDITOR && UNITY_STANDALONE_OSX || CT_DEVELOP
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

namespace Crosstales.Common.Util
{
   /// <summary>Post processor for macOS.</summary>
   public static class CTPMacOSPostProcessor
   {
      public static bool REWRITE_BUNDLE = false; //change it to true if the bundle-id should be changed

      private const string ID = "com.crosstales.procstart";

      [PostProcessBuildAttribute(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (BaseHelper.isMacOSPlatform)
         {
            //remove all meta-files
            try
            {
               string[] files = FileHelper.GetFiles(pathToBuiltProject, true, "meta");

               foreach (string file in files)
               {
                  try
                  {
                     FileHelper.DeleteFile(file);
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
#if CT_FB
            if (Crosstales.FB.EditorUtil.EditorConfig.MACOS_MODIFY_BUNDLE)
#else
            if (REWRITE_BUNDLE)
#endif
            {
               try
               {
                  string[] files = FileHelper.GetFiles(pathToBuiltProject, true, "plist");

                  foreach (string file in files)
                  {
                     try
                     {
                        string content = Crosstales.Common.Util.FileHelper.ReadAllText(file);

                        if (content.Contains(ID) && !content.Contains($"{ID}."))
                        {
                           content = content.Replace(ID, $"{ID}.{System.DateTime.Now:yyyyMMddHHmmss}");
                           FileHelper.WriteAllText(file, content);
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
// © 2021-2024 crosstales LLC (https://www.crosstales.com)