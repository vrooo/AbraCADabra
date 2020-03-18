using System.ComponentModel;
using System.Windows;

namespace AbraCADabra
{
    public abstract class MeshManager : INotifyPropertyChanged
    {
        public Mesh Mesh { get; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
        public abstract string DefaultName { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected MeshManager(Mesh mesh)
        {
            Mesh = mesh;
            _name = DefaultName;
        }

        public abstract void Update();
    }
}
