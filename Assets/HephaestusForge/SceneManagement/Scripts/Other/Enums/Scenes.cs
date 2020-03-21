using System;

namespace HephaestusForge
{
    namespace SceneManagement
    {
        /// <summary>
        /// Click on the add scene to build settings and this will be automatically updated.
        /// </summary>
        [Flags]
        public enum Scenes 
        { 
            MainMenu = 1 << 0,
            PlaceHolder = 1 << 1,
        }
    }
}
