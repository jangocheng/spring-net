#region Licence

/*
 * Copyright � 2002-2011 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections.Generic;

using Spring.Logging;
using Spring.Context;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Util;

namespace Spring.Data.Common
{
    /// <summary>
    /// Create DbProviders based on configuration information in assembly resource
    /// dbproviders.xml
    ///
    ///
    /// </summary>
    /// <remarks>
    /// TODO: Provide over-ride resource location.
    /// TODO: Add error codes to provider.
    /// </remarks>
    /// <author>Mark Pollack (.NET)</author>
    public class DbProviderFactory
    {
    	#region Constants

        /// <summary>
        /// The shared log instance for this class (and derived classes).
        /// </summary>
        protected static ILogger log = LoggingManager.GetLogger(typeof(DbProviderFactory));

        private static readonly string DBPROVIDER_DEFAULT_RESOURCE_NAME =
            "assembly://" + typeof(DbProviderFactory).Assembly.FullName + "/Spring.Data.Common/dbproviders.xml";

        public static string DBPROVIDER_ADDITIONAL_RESOURCE_NAME =
            "file://dbProviders.xml";

		private static readonly string DBPROVIDER_CONTEXTNAME = "DBPROVIDERFACTORY_CONTEXT";

        #endregion

	    #region Fields

        private volatile static XmlApplicationContext ctx;

        #endregion

        #region Constructor (s)
        /// <summary>
        /// Initializes a new instance of the <see cref="DbProviderFactory"/> class.
        /// </summary>
        static DbProviderFactory()
        {
        }

        #endregion


        /// <summary>
        /// Gets the DbProvider given an identifying name.
        /// </summary>
        /// <remarks>
        /// Familiar names for the .NET 2.0 provider model are supported, i.e.
        /// System.Data.SqlClient.  Refer to the documentation for a complete
        /// listing of supported DbProviders and their names.  You may
        /// also use the method GetDbProviderClasses or obtain the
        /// underlying IApplicationContext to progammatically obtain information
        /// about supported providers.
        /// </remarks>
        /// <param name="providerInvariantName">Name of the provider invariant.</param>
        /// <returns></returns>
        public static IDbProvider GetDbProvider(string providerInvariantName)
        {
            InitializeDbProviderFactoryIfNeeded();

            return ctx.GetObject(providerInvariantName, typeof(IDbProvider)) as IDbProvider;

        }

        /// <summary>
        /// Gets the application context that contains the definitions of the
        /// various providers.
        /// </summary>
        /// <remarks>This method should rarely, if ever, be used in
        /// application code.  It is used by the framework itself
        /// to map other data access products abstractions for a 'DbProvider/DataSource'
        /// onto Spring's model.</remarks>
        /// <value>The application context.</value>
        public static IApplicationContext ApplicationContext
        {
            get
            {
                InitializeDbProviderFactoryIfNeeded();
                return ctx;
            }
        }

        //TODO GetDbProviderClasses
        private static void InitializeDbProviderFactoryIfNeeded()
        {
            if (ctx == null)
            {
				lock(typeof(DbProviderFactory))
				{
					if (ctx == null)
					{
						try
						{
							ConfigurableResourceLoader loader = new ConfigurableResourceLoader(DBPROVIDER_ADDITIONAL_RESOURCE_NAME);


							if (loader.GetResource(DBPROVIDER_ADDITIONAL_RESOURCE_NAME).Exists)
							{
								#region Instrumentation
								if (log.IsDebugEnabled)
								{
									log.Debug("Loading additional DbProviders from " + DBPROVIDER_ADDITIONAL_RESOURCE_NAME);
								}
								#endregion
								ctx = new XmlApplicationContext(DBPROVIDER_CONTEXTNAME, true, new string[] { DBPROVIDER_DEFAULT_RESOURCE_NAME,
								                                               DBPROVIDER_ADDITIONAL_RESOURCE_NAME});
							}
							else
							{
								ctx = new XmlApplicationContext(DBPROVIDER_CONTEXTNAME, true, new string[] { DBPROVIDER_DEFAULT_RESOURCE_NAME });
							}

							IList<string> dbProviderNames = ctx.GetObjectNames<IDbProvider>();
							if (log.IsInfoEnabled)
							{
								log.Info(String.Format("{0} DbProviders Available. [{1}]", dbProviderNames.Count, StringUtils.CollectionToCommaDelimitedString(dbProviderNames)));
							}
						}
						catch (Exception e)
						{
							log.Error("Error processing " + DBPROVIDER_DEFAULT_RESOURCE_NAME, e);
							throw;
						}
					}					
				}
            }
        }
    }
}
