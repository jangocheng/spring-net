using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spring.Collections;
using Spring.Logging;
using Spring.Objects.Factory.Config;
using Spring.Util;

namespace Spring.Objects.Factory.Support
{
    public class DisposableObjectAdapter : IDisposable
    {
        private readonly ILogger logger = LogManager.GetLogger(typeof(DisposableObjectAdapter));

        private readonly MethodInfo destroyMethod;

        private readonly string destroyMethodName;

        private readonly object instance;

        private readonly bool invokeDisposableObject;

        private readonly string objectName;

        private readonly List<IDestructionAwareObjectPostProcessor> objectPostProcessors;

        /// <summary>
        ///     Create a new DisposableBeanAdapter for the given bean.
        /// </summary>
        /// <param name="instance">The bean instance (never <code>null</code>).</param>
        /// <param name="objectName">Name of the bean.</param>
        /// <param name="objectDefinition">The merged bean definition.</param>
        /// <param name="postProcessors">the List of BeanPostProcessors (potentially IDestructionAwareBeanPostProcessor), if any.</param>
        public DisposableObjectAdapter(object instance, string objectName, RootObjectDefinition objectDefinition,
            ISet postProcessors)
        {
            AssertUtils.ArgumentNotNull(instance, "Disposable object must not be null");

            this.instance = instance;
            this.objectName = objectName;
            invokeDisposableObject =
                this.instance is IDisposable; // && !beanDefinition.IsExternallyManagedDestroyMethod("destroy"));

            if (null == objectDefinition)
            {
                return;
            }

            InferDestroyMethodIfNecessary(objectDefinition);

            string definedDestroyMethodName = objectDefinition.DestroyMethodName;

            if (definedDestroyMethodName != null &&
                !(invokeDisposableObject && "Destroy".Equals(definedDestroyMethodName))
            ) // && !beanDefinition.isExternallyManagedDestroyMethod(destroyMethodName)) 
            {
                destroyMethodName = definedDestroyMethodName;
                destroyMethod = DetermineDestroyMethod();
                if (destroyMethod == null)
                {
                    //TODO: add support for Enforcing Destroy Method
                    //if (beanDefinition.IsEnforceDestroyMethod()) {
                    //    throw new BeanDefinitionValidationException("Couldn't find a destroy method named '" +
                    //                                                destroyMethodName + "' on bean with name '" + beanName + "'");
                    //}
                }
                else
                {
                    Type[] paramTypes = ReflectionUtils.GetParameterTypes(destroyMethod);
                    if (paramTypes.Length > 1)
                    {
                        throw new ObjectDefinitionValidationException("Method '" + definedDestroyMethodName +
                                                                      "' of object '" +
                                                                      objectName +
                                                                      "' has more than one parameter - not supported as Destroy Method");
                    }
                    if (paramTypes.Length == 1 && !(paramTypes[0] == typeof(bool)))
                    {
                        throw new ObjectDefinitionValidationException("Method '" + definedDestroyMethodName +
                                                                      "' of object '" +
                                                                      objectName +
                                                                      "' has a non-boolean parameter - not supported as Destroy Method");
                    }
                }
            }
            objectPostProcessors = FilterPostProcessors(postProcessors);
        }


        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (objectPostProcessors != null && objectPostProcessors.Count != 0)
            {
                foreach (IDestructionAwareObjectPostProcessor processor in objectPostProcessors)
                    try
                    {
                        processor.PostProcessBeforeDestruction(instance, objectName);
                    }

                    catch (Exception ex)
                    {
                        logger.ErrorFormat(
                            string.Format("Error during execution of {0}.PostProcessBeforeDestruction for object {1}",
                                processor.GetType().Name, objectName), ex);
                    }
            }

            if (invokeDisposableObject)
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Invoking Dispose() on object with name '" + objectName + "'");
                }
                try
                {
                    ((IDisposable) instance).Dispose();
                }

                catch (Exception ex)
                {
                    string msg = "Invocation of Dispose method failed on object with name '" + objectName + "'";
                    if (logger.IsDebugEnabled)
                    {
                        logger.Warn(msg, ex);
                    }
                    else
                    {
                        logger.Warn(msg + ": " + ex);
                    }
                }
            }

            if (destroyMethod != null)
            {
                InvokeCustomDestroyMethod(destroyMethod);
            }
            else if (destroyMethodName != null)
            {
                MethodInfo methodToCall = DetermineDestroyMethod();
                if (methodToCall != null)
                {
                    InvokeCustomDestroyMethod(methodToCall);
                }
            }
        }

        private void InferDestroyMethodIfNecessary(RootObjectDefinition beanDefinition)
        {
            if ("(Inferred)".Equals(beanDefinition.DestroyMethodName))
            {
                try
                {
                    MethodInfo candidate = ReflectionUtils.GetMethod(instance.GetType(), "Close", null);
                    if (candidate.IsPublic)
                    {
                        beanDefinition.DestroyMethodName = candidate.Name;
                    }
                }
                catch (MissingMethodException)
                {
                    // no candidate destroy method found
                    beanDefinition.DestroyMethodName = null;
                }
            }
        }

        /// <summary>
        ///     Search for all <see cref="IDestructionAwareObjectPostProcessor" />s in the List.
        /// </summary>
        /// <param name="postProcessors">The List to search.</param>
        /// <returns>the filtered List of IDestructionAwareObjectPostProcessors.</returns>
        private List<IDestructionAwareObjectPostProcessor> FilterPostProcessors(ISet postProcessors)
        {
            List<IDestructionAwareObjectPostProcessor> filteredPostProcessors = null;
            if (postProcessors != null && postProcessors.Count != 0)
            {
                filteredPostProcessors = new List<IDestructionAwareObjectPostProcessor>(postProcessors.Count);
                filteredPostProcessors.AddRange(postProcessors.OfType<IDestructionAwareObjectPostProcessor>());
            }
            return filteredPostProcessors;
        }


        private MethodInfo DetermineDestroyMethod()
        {
            try
            {
                return FindDestroyMethod();
            }

            catch (ArgumentException ex)
            {
                throw new ObjectDefinitionValidationException(
                    "Couldn't find a unique Destroy Method on object with name '" +
                    objectName + ": " + ex.Message);
            }
        }

        private MethodInfo FindDestroyMethod()
        {
            try
            {
                return ReflectionUtils.GetMethod(instance.GetType(), destroyMethodName, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///     Invokes the custom destroy method.
        /// </summary>
        /// <param name="customDestroyMethod">The custom destroy method.</param>
        /// Invoke the specified custom destroy method on the given bean.
        /// This implementation invokes a no-arg method if found, else checking
        /// for a method with a single boolean argument (passing in "true",
        /// assuming a "force" parameter), else logging an error.
        private void InvokeCustomDestroyMethod(MethodInfo customDestroyMethod)
        {
            Type[] paramTypes = ReflectionUtils.GetParameterTypes(customDestroyMethod);
            object[] args = new object[paramTypes.Length];
            if (paramTypes.Length == 1)
            {
                args[0] = true;
            }
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Invoking destroy method '" + destroyMethodName +
                             "' on object with name '" + objectName + "'");
            }
            try
            {
                customDestroyMethod.Invoke(instance, args);
            }
            catch (TargetInvocationException ex)
            {
                string msg = "Invocation of destroy method '" + destroyMethodName +
                             "' failed on object with name '" + objectName + "'";
                if (logger.IsDebugEnabled)
                {
                    logger.Warn(msg, ex.InnerException);
                }
                else
                {
                    logger.Warn(msg + ": " + ex.InnerException);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Couldn't invoke destroy method '" + destroyMethodName +
                             "' on object with name '" + objectName + "'", ex);
            }
        }
    }
}