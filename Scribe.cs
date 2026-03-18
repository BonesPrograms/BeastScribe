using XRL.World;
using System;
using System.Reflection;

namespace BeastScribe.Scribes
{
  
    public abstract class BaseScribe<T>
    {
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private protected BaseScribe()
        {

        }

        public abstract void AccessInstance(T serializer, object instance, Type type);
        public void Scribe(T serializer, Effect instance)
        {
            LoopInheritance(serializer, instance, typeof(Effect));
        }

        public void Scribe(T serializer, IPart instance)
        {
            LoopInheritance(serializer, instance, typeof(IPart));
        }

        public void Scribe(T serializer, IComposite instance)
        {
            LoopInheritance(serializer, instance, null);
        }
        public void LoopInheritance(T serializer, object instance, Type limit)
        {
            Type type = instance.GetType();
            while (type != limit)
            {
                AccessInstance(serializer, instance, type);
                type = type.BaseType;
            }
        }


    }

    public sealed class ScribeWriter : BaseScribe<SerializationWriter>
    {
        public static readonly ScribeWriter Writer = new();
        ScribeWriter()
        {

        }
        public override void AccessInstance(SerializationWriter writer, object instance, Type type)
        {
            writer.WriteNamedFields(instance, type, Flags);
        }


    }


    public sealed class ScribeReader : BaseScribe<SerializationReader>
    {
        public static readonly ScribeReader Reader = new();
        ScribeReader()
        {

        }
        public override void AccessInstance(SerializationReader reader, object instance, Type type)
        {
            FieldInfo[] fields = type.GetFields(Flags);
            int serializedCount = reader.ReadOptimizedInt32();
            for (int i = 0; i < serializedCount; i++)
            {
                string serializedName = reader.ReadOptimizedString();
                object serializedValue = reader.ReadObject();
                for (int x = 0; x < fields.Length; x++)
                {
                    FieldInfo field = fields[x];
                    if (field.Name == serializedName)
                    {
                        field.SetValue(instance, serializedValue);
                        break;
                    }
                }
            }
        }
    }

}

