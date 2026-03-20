using XRL.World;
using System;
using System.Reflection;
using XRL.World.Parts.Mutation;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using XRL.World.Parts;

namespace BeastScribe.Scribes
{

    public abstract class BaseScribe<T>
    {
        protected const BindingFlags InheritorFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//will include all inherited protected and public fields
        protected const BindingFlags BaseTypeFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly; //will include protected fields that the inheritor has 
                                                                                                                                 // already serialized, needs further sifting to only be private                                                                
        public BaseScribe()
        {

        }
        protected abstract void AccessInstance(T serializer, object instance, Type type, int count, bool privateExcluded);

        public void Scribe(T serializer, Effect instance)
        {
            LoopInheritance(serializer, instance, typeof(Effect));
        }

        public void Scribe(T serializer, IPart instance)
        {
            LoopInheritance(serializer, instance, typeof(IPart));
        }

        public void Scribe(T serializer, IComposite instance, Type limit = null) //null means it will stop at System.Object
        {
            LoopInheritance(serializer, instance, limit);
        }

        protected void LoopInheritance(T serializer, object instance, Type limit, Type[] excludePrivate = null)
        {
            int count = 0;
            Type type = instance.GetType();
            bool excludePrivateFields = false;
            while (type != limit)
            {
                if (excludePrivate != null)
                {
                    excludePrivateFields = false;
                    for (int i = 0; i < excludePrivate.Length; i++)
                    {
                        if (excludePrivate[i] == type)
                            excludePrivateFields = true;
                    }
                }
                AccessInstance(serializer, instance, type, count, excludePrivateFields);
                type = type.BaseType;
                count++;

            }
        }

    }

    public sealed class ScribeWriter : BaseScribe<SerializationWriter>
    {

        const FieldAttributes Mask = FieldAttributes.NotSerialized | FieldAttributes.Static | FieldAttributes.Literal;
        public static readonly ScribeWriter Writer = new();
        ScribeWriter()
        {

        }

        public void SafeWrite(SerializationWriter serializer, IPart instance, Type[] excludePrivate)
        {
            LoopInheritance(serializer, instance, typeof(IPart), excludePrivate);
        }

        public void SafeWrite(SerializationWriter serializer, Effect instance, Type[] excludePrivate)
        {
            LoopInheritance(serializer, instance, typeof(Effect), excludePrivate);
        }
        protected override void AccessInstance(SerializationWriter writer, object instance, Type type, int count, bool privateExcluded)
        {
            BindingFlags flags = count == 0 ? InheritorFlags : BaseTypeFlags;
            FieldInfo[] fields = type.GetFields(flags);
            int size = fields.Length;
            int serializeableCount = 0;
            for (int i = 0; i < size; i++)
            {
                FieldInfo field = fields[i];
                if (count == 0)
                {
                    if ((field.Attributes & Mask) == 0)
                        serializeableCount++;
                }
                else if (!privateExcluded && field.IsPrivate && ((field.Attributes & Mask) == 0))
                    serializeableCount++;
            }
            writer.WriteOptimized(serializeableCount);
            for (int i = 0; i < size; i++)
            {
                if (serializeableCount <= 0)
                    break;
                FieldInfo field = fields[i];
                if (count == 0)
                {
                    if ((field.Attributes & Mask) == 0)
                    {
                        writer.WriteOptimized(field.Name);
                        writer.WriteObject(field.GetValue(instance));
                        serializeableCount--;
                    }
                }
                else if (!privateExcluded && field.IsPrivate && ((field.Attributes & Mask) == 0)) //ensured that only private fields of base classes are serialized, not protected ones
                {
                    writer.WriteOptimized(field.Name);
                    writer.WriteObject(field.GetValue(instance));
                    serializeableCount--;
                }

            }
        }

    }


    public sealed class ScribeReader : BaseScribe<SerializationReader>
    {
        public static readonly ScribeReader Reader = new();
        ScribeReader()
        {

        }
        protected override void AccessInstance(SerializationReader reader, object instance, Type type, int count, bool privateExcluded)
        {
            BindingFlags flags = count == 0 ? InheritorFlags : BaseTypeFlags;
            FieldInfo[] fields = type.GetFields(flags);
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

