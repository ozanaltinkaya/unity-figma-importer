﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Cdm.Figma.UI.Utils
{
    public class BindingResult
    {
        public bool hasErrors => errors.Any();
        public List<BindingError> errors { get; } = new List<BindingError>();
    }

    public readonly struct BindingError
    {
        public MemberInfo memberInfo { get; }
        public string message { get; }

        public BindingError(MemberInfo memberInfo, string message)
        {
            this.memberInfo = memberInfo;
            this.message = message;
        }

        public override string ToString()
        {
            return $"Binding of '{memberInfo.Name}' failed. {message}";
        }
    }

    public class FigmaNodeBinder
    {
        public static BindingResult Bind(object obj, FigmaNode node)
        {
            var bindingResult = new BindingResult();

            BindFigmaNodeAttributes(obj, node, bindingResult);
            BindFigmaResourceAttributes(obj, node, bindingResult);
            
            if (obj is IFigmaNodeBinder nodeBinder)
            {
                nodeBinder.OnBind(node);
            }

            return bindingResult;
        }

        private static void BindFigmaResourceAttributes(object obj, FigmaNode node, BindingResult bindingResult)
        {
            var members = obj.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var member in members)
            {
                if (!member.MemberType.HasFlag(MemberTypes.Field) &&
                    !member.MemberType.HasFlag(MemberTypes.Property))
                    continue;
                
                var figmaResourceAttribute = (FigmaResourceAttribute)member.GetCustomAttributes()
                    .FirstOrDefault(x => x.GetType() == typeof(FigmaResourceAttribute));

                if (figmaResourceAttribute == null)
                    continue;
                    
                var fieldType = GetUnderlyingType(member);
                var resource = Resources.Load(figmaResourceAttribute.path, fieldType);
                    
                if (resource != null)
                {
                    SetMemberValue(member, obj, resource);
                } 
                else if (figmaResourceAttribute.isRequired)
                {
                    bindingResult.errors.Add(
                        new BindingError(member,
                            $"Specified resource '{figmaResourceAttribute.path}' with the field type " + 
                            $"'{fieldType}' could not be found."));
                }
            }
        }

        private static void BindFigmaNodeAttributes(object obj, FigmaNode node, BindingResult bindingResult)
        {
            var members = obj.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var member in members)
            {
                if (member.MemberType.HasFlag(MemberTypes.Field) ||
                    member.MemberType.HasFlag(MemberTypes.Property))
                {
                    var figmaNodeAttribute = (FigmaNodeAttribute)member.GetCustomAttributes()
                        .FirstOrDefault(x => x.GetType() == typeof(FigmaNodeAttribute));
                    
                    if (figmaNodeAttribute != null)
                    {
                        var bindingKey = member.Name;

                        if (!string.IsNullOrEmpty(figmaNodeAttribute.bindingKey))
                        {
                            bindingKey = figmaNodeAttribute.bindingKey;
                        }

                        var fieldType = GetUnderlyingType(member);
                        //Debug.Log($"{member.Name}, {bindingKey}, {fieldType.Name}");

                        var isComponentType = typeof(UnityEngine.Component).IsAssignableFrom(fieldType);
                        var isGameObjectType = fieldType == typeof(GameObject);
                        
                        if (isComponentType)
                        {
                            var target = node.Query(bindingKey, fieldType);
                            if (target == null)
                            {
                                // Add component and bind it on the fly if it does not exist.
                                var child = node.Query(bindingKey);
                                if (child != null && child.gameObject != node.gameObject)
                                {
                                    var childObj = child.gameObject.AddComponent(fieldType);
                                    var result = Bind(childObj, child);
                                    
                                    target = childObj;
                                    bindingResult.errors.AddRange(result.errors);
                                }
                            }
                            
                            if (target != null)
                            {
                                SetMemberValue(member, obj, target);
                            } 
                            else if (figmaNodeAttribute.isRequired)
                            {
                                bindingResult.errors.Add(
                                    new BindingError(member,
                                        $"Specified node '{bindingKey}' with the field type '{fieldType}' could not be found."));
                            }
                        }
                        else if (isGameObjectType)
                        {
                            var target = node.Query(bindingKey);
                            if (target != null)
                            {
                                SetMemberValue(member, obj, target.gameObject);
                            }
                            else if (figmaNodeAttribute.isRequired)
                            {
                                bindingResult.errors.Add(
                                    new BindingError(member,
                                        $"Specified node '{bindingKey}' with the type of '{nameof(GameObject)}' could not be found."));
                            }
                        }
                        else
                        {
                            bindingResult.errors.Add(
                                new BindingError(member,
                                    $"{nameof(FigmaNodeAttribute)} only valid for types that sub class of " + 
                                    $"{typeof(UnityEngine.Component)} or is on {nameof(GameObject)}." +
                                    $" Got '{fieldType}'."));
                        }
                    }
                }
            }
        }

        private static Type GetUnderlyingType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException(
                        "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo");
            }
        }

        private static void SetMemberValue(MemberInfo member, object target, object value)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(target, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(target, value, null);
                    break;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo", nameof(member));
            }
        }
    }
}