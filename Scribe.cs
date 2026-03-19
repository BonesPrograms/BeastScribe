using XRL.World;
using System;
using System.Reflection;
using XRL.World.Parts.Mutation;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace BeastScribe.Scribes
{

    public abstract class BaseScribe<T>
    {
        static readonly FieldInfo[] BaseMutationFields = GetBaseMutationFields();
        protected const BindingFlags InheritorFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//will include all inherited protected and public fields
        protected const BindingFlags BaseTypeFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly; //will include protected fields that the inheritor has 
                                                                                                                                 // already serialized, needs further sifting to only be private                                                                
        public BaseScribe()
        {

        }
        protected abstract void AccessInstance(T serializer, object instance, Type type, int count);

        public void Scribe(T serializer, BaseMutation mutation)
        {
            LoopInheritance(serializer, mutation, typeof(IPart));
        }
        public void Scribe(T serializer, Effect instance)
        {
            LoopInheritance(serializer, instance, typeof(Effect));
        }

        public void Scribe(T serializer, IPart instance)
        {
            if (DebugCheck(instance))
                LoopInheritance(serializer, instance, typeof(IPart));
        }

        public void Scribe(T serializer, IComposite instance)
        {
            LoopInheritance(serializer, instance, null);
        }

        static bool DebugCheck(IComponent<GameObject> instance) => instance.GetBasisGameObject().IsPlayer();
        void LoopInheritance(T serializer, object instance, Type limit)
        {
            int count = 0;
            Type type = instance.GetType();
            while (type != limit)
            {

                AccessInstance(serializer, instance, type, count);
                type = type.BaseType;
                count++;

            }
        }
        static FieldInfo[] GetBaseMutationFields() //gets public only fields for baseMutation on program start
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            FieldInfo[] fields = typeof(BaseMutation).GetFields(flags).Where(x => !x.IsNotSerialized).ToArray();
            return fields;
        }


        void LoopMutation(T serializer, BaseMutation instance)
        {
            Type type = instance.GetType();

        }

    }

    public sealed class ScribeWriter : BaseScribe<SerializationWriter>
    {

        const FieldAttributes Attributes = FieldAttributes.NotSerialized | FieldAttributes.Static | FieldAttributes.Literal;
        public static readonly ScribeWriter Writer = new();
        ScribeWriter()
        {

        }
        protected override void AccessInstance(SerializationWriter writer, object instance, Type type, int count)
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
                    if ((field.Attributes & Attributes) == 0)
                        serializeableCount++;
                }
                else if (field.IsPrivate && ((field.Attributes & Attributes) == 0))
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
                    if ((field.Attributes & Attributes) == 0)
                    {
                        writer.WriteOptimized(field.Name);
                        writer.WriteObject(field.GetValue(instance));
                        serializeableCount--;
                    }
                }
                else if (field.IsPrivate && ((field.Attributes & Attributes) == 0)) //ensured that only private fields of base classes are serialized, not protected ones
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
        protected override void AccessInstance(SerializationReader reader, object instance, Type type, int count)
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

