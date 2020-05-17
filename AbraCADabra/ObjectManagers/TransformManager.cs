using OpenTK;
using System;
using System.ComponentModel;

namespace AbraCADabra
{
    public abstract class FloatTransformManager : TransformManager<float>
    {
        protected FloatTransformManager(FloatTransform transform) : base(transform) { }
    }

    public abstract class TransformManager<T> : TransformManager where T : struct
    {
        protected TransformManager(Transform<T> transform) : base(transform) { }
    }

    public abstract class TransformManager : INotifyPropertyChanged, IDisposable
    {
        public Transform Transform { get; }

        #region Model properties
        public virtual float PositionX
        {
            get { return Transform.Position.X; }
            set
            {
                Transform.Position.X = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionX"));
            }
        }
        
        public virtual float PositionY
        {
            get { return Transform.Position.Y; }
            set
            {
                Transform.Position.Y = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionY"));
            }
        }
        
        public virtual float PositionZ
        {
            get { return Transform.Position.Z; }
            set
            {
                Transform.Position.Z = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionZ"));
            }
        }
        
        public float RotationX
        {
            get { return Transform.Rotation.X; }
            set
            {
                Transform.Rotation.X = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationX"));
            }
        }
        
        public float RotationY
        {
            get { return Transform.Rotation.Y; }
            set
            {
                Transform.Rotation.Y = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationY"));
            }
        }
        
        public float RotationZ
        {
            get { return Transform.Rotation.Z; }
            set
            {
                Transform.Rotation.Z = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationZ"));
            }
        }
        
        public float ScaleX
        {
            get { return Transform.Scale.X; }
            set
            {
                Transform.Scale.X = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX"));
            }
        }
        
        public float ScaleY
        {
            get { return Transform.Scale.Y; }
            set
            {
                Transform.Scale.Y = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY"));
            }
        }
        
        public float ScaleZ
        {
            get { return Transform.Scale.Z; }
            set
            {
                Transform.Scale.Z = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleZ"));
            }
        }
        #endregion

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
        protected abstract int instanceCounter { get; } // TODO: better solution?

        public virtual bool Deletable => true;

        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void ManagerDisposingEventHandler(TransformManager sender);
        public event ManagerDisposingEventHandler ManagerDisposing;

        protected TransformManager(Transform transform)
        {
            Transform = transform;
            _name = DefaultName;
            int instance = instanceCounter;
            if (instance > 0)
            {
                _name += " (" + instance.ToString() + ")";
            }
        }

        public virtual void Translate(float x, float y, float z)
        {
            Transform.Translate(x, y, z);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionZ"));
        }

        public virtual void Rotate(float x, float y, float z)
        {
            Transform.Rotate(x, y, z);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationZ"));
        }
        public virtual void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center)
        {
            Transform.RotateAround(xAngle, yAngle, zAngle, center);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionZ"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotationZ"));
        }

        public virtual void ScaleUniform(float delta)
        {
            Transform.ScaleUniform(delta);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleZ"));
        }

        public Vector3 GetScreenSpaceCoords(Camera camera, float width, float height)
            => Transform.GetScreenSpaceCoords(camera, width, height);

        public virtual bool TestHit(Camera camera, float width, float height, float x, float y, out float z)
            => Transform.TestHit(camera, width, height, x, y, out z);

        public abstract void Update(); // TODO: this should set update flag which is checked before render

        public virtual void Render(ShaderManager shader)
        {
            Transform.Render(shader);
        }

        public virtual void Dispose()
        {
            ManagerDisposing?.Invoke(this);
            Transform.Dispose();
        }
    }
}
