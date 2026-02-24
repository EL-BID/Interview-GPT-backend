# Interview GPT (API)

Interview GPT es un aplicativo que te permite usar modelos de inteligencia Artificial para asistir en la captura de información a través de entrevistas. 

**Interview GPT** es un proyecto compuesto por tres repositorios:
 
- **Backend**: [EL-BID/Interview-GPT-backend](https://github.com/EL-BID/Interview-GPT-backend)
- **AI API**: [EL-BID/Interview-GPT-AI-API](https://github.com/EL-BID/Interview-GPT-AI-API)
- **Interfaz Web (UI)**: [EL-BID/Interview-GPT-UI](https://github.com/EL-BID/Interview-GPT-UI)

El Backend se utiliza para gestionar la información que se almacena en una base de datos SQL. Está construido en C# en una arquitectura de microservicios haciendo uso de la plataforma Azure Functions.

## Infraestructura como Código

Adicionalmente, este proyecto incluye archivos de Infraestructura como Código el cual ayuda provisionar los recursos de Azure para los tres repositorios mencionados.

## Tecnologías Utilizadas

- C#
- .dot core 8
- MS SQL
- Azure Active Directory

## Dependencias

### Base de datos

El proyecto fue adaptado para usar una base de datos en MS SQL Server. Se utiliza la librería Entity Framework para la interacción con la base de datos. Sin embargo, no se ha configurado un proceso para hacer migraciones automáticas para los esquemas de datos.

Será requerido crear una base de datos y ejecutar el script de creación de base de datos ubicado en el archivo database/db.sql de este repositorio.

### Autenticación

El API dispone de funciones (endpoints) públicas y privdas. Las funciones referentes al módulo administrativo o de gestión son privados. 

Por facilidad, las funciones públicas se encontrarán en los archivos cuyo nombre tiene el prefijo "Public", estas son utilizadas para encuestas que no requiere autenticación e identifican a usuarios y sus sesiones vía un código de invitación.

Las funciones privadas fueron configuradas para usar una autenticación con Azure Active Directory y requieren del envío de un Token JWT validado.


### Envío de correos

El envío de correos es una funcionalidad que se implementó para informar a usuarios que fueron invitados a contenstar una entrevista. Para el envío se ha configurado el uso de servicios externos. Actualmente es soportado [MailGun](https://www.mailgun.com) y [SendGrid](https://www.sendgrid.com). Ver archivos Utils/EmailUtils.cs


## Configuraciones

Para poder ejecutar el código en un ambiente local es preciso contar con un archivo llamado local.settings.json con ubicación en el directorio raíz.

```json
{
  "IsEncrypted": true,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true", //uses azurite on local environment.
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "jwt_audience": "api://[app-id]",
    "jwt_authority": "https://login.microsoftonline.com/[azure-tenant-id]/v2.0",
    "jwt_issuer": "https://sts.windows.net/[azure-tenant-id]/",
    "sqldb_connection": "[connection url]",
    "BASE_URL": "[front-end-base-url]", // utilizada para generación de URLs para envío de correos.
    "MAIL_API_KEY": "[mail-api-key]", // utilizada para la configuración de envío de correos, es soportado únicamente Mailgun y Sendgrid
    "MAIL_SERVICE": "[mailgun|sendgrid]"
  },
  "ConnectionStrings": {
    "SQLConnectionString": {
      "ConnectionString": "",
      "ProviderName": ""
    }
  }
}
```

## Licencia

Todo el material distruido por este repositorio tiene licencia MIT, más detalles en LICENSE.md


## Infraestructura como código (IaC)

Las plantillas se encuentra en el directorio con nombre "iac". Dentro del directorio se pueden visualizar dos archivos: template.json y template.parameters.json

Es importante que se modifique el archivo template.parameters.json actualizando, para cada parámetro, el campo "value", el cual determina el nombre que tendrá el recurso creado en Azure.


A continuación se detallan los pasos para crear recursos en un tenant de Azure utilizando la CLI en Windows:

1. Crear el Resource Group
Ejecuta el siguiente comando en la terminal de Windows (PowerShell o CMD) para crear un Resource Group:

```
az group create --name <nombre-del-resource-group> --location <ubicacion>
```

Ejemplo:

```
az group create --name rg-interview-gpt --location eastus
```

2. Desplegar la plantilla ARM
Para desplegar la plantilla ARM ubicada en iac/template.json usando los parámetros definidos en iac/template.parameters.json, ejecuta el siguiente comando, asegurándote de referenciar el Resource Group creado previamente:

```
az deployment group create ^
  --resource-group <nombre-del-resource-group> ^
  --template-file iac/template.json ^
  --parameters iac/template.parameters.json
```

Ejemplo:

```
az deployment group create ^
  --resource-group rg-interview-gpt ^
  --template-file iac/template.json ^
  --parameters iac/template.parameters.json
```

Esto iniciará el proceso de despliegue de los recursos definidos en la plantilla ARM. Verifica que los parámetros en iac/template.parameters.json estén correctamente configurados antes de ejecutar el comando.


## Contribuidores

- José Daniel Zárate Martínez
- Raúl Ignacio Cerrato
- Alejandra Pérez Ortega
- Miluska Pajuelo


## Acknowledgments / Reconocimientos

**Copyright © [2025]. Inter-American Development Bank ("IDB"). Authorized Use.**  
The procedures and results obtained based on the execution of this software are those programmed by the developers and do not necessarily reflect the views of the IDB, its Board of Executive Directors or the countries it represents.

**Copyright © [2025]. Banco Interamericano de Desarrollo ("BID"). Uso Autorizado.**  
Los procedimientos y resultados obtenidos con la ejecución de este software son los programados por los desarrolladores y no reflejan necesariamente las opiniones del BID, su Directorio Ejecutivo ni los países que representa.

### Support and Usage Documentation / Documentación de Soporte y Uso

**Copyright © [2025]. Inter-American Development Bank ("IDB").** The Support and Usage Documentation is licensed under the Creative Commons License CC-BY 4.0 license. The opinions expressed in the Support and Usage Documentation are those of its authors and do not necessarily reflect the opinions of the IDB, its Board of Executive Directors, or the countries it represents.

**Copyright © [2025]. Banco Interamericano de Desarrollo (BID).** La Documentación de Soporte y Uso está licenciada bajo la licencia Creative Commons CC-BY 4.0. Las opiniones expresadas en la Documentación de Soporte y Uso son las de sus autores y no reflejan necesariamente las opiniones del BID, su Directorio Ejecutivo ni los países que representa.

### AI-Powered Services Disclaimer / Exención de responsabilidad por Servicios Impulsados por IA

The Software may include features which use, are powered by, or are an artificial intelligence system (“AI-Powered Services”), and as a result, the services provided via the Software may not be completely error-free or up to date. Additionally, the User acknowledges that due to the incorporation of AI-Powered Services in the Software, the Software may not dynamically (in “real time”) retrieve information and that, consequently, the output provided to the User may not account for events, updates, or other facts that have occurred or become available after the Software was trained. Accordingly, the User acknowledges that the use of the Software, and that any actions taken or reliance on such products, are at the User’s own risk, and the User acknowledges that the User must independently verify any information provided by the Software.

El Software puede incluir funciones que utilizan, están impulsadas por o son un sistema de inteligencia artificial (“Servicios Impulsados por IA”) y, como resultado, los servicios proporcionados a través del Software pueden no estar completamente libres de errores ni actualizados. Además, el Usuario reconoce que, debido a la incorporación de Servicios Impulsados por IA en el Software, este puede no recuperar información dinámicamente (en “tiempo real”) y que, en consecuencia, la información proporcionada al Usuario puede no reflejar eventos, actualizaciones u otros hechos que hayan ocurrido o estén disponibles después del entrenamiento del Software. En consecuencia, el Usuario reconoce que el uso del Software, y que cualquier acción realizada o la confianza depositada en dichos productos, se realiza bajo su propio riesgo, y reconoce que debe verificar de forma independiente cualquier información proporcionada por el Software.
