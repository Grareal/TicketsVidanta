# Configuración de Oracle Hospitality Integration Platform (OHIP)

La implementación sigue la documentación y las especificaciones oficiales de Oracle Hospitality:

- Guía de llamadas y encabezados obligatorios:
  https://docs.oracle.com/en/industries/hospitality/integration-platform/ohipu/c_calling_oracle_hospitality_property_apis_ocim.htm
- Especificaciones REST oficiales:
  https://github.com/oracle/hospitality-api-docs
- Especificación de reservas `rsv/v1`:
  https://github.com/oracle/hospitality-api-docs/blob/main/rest-api-specs/property/v1/rsv.json
- La carga acordada para este proyecto usa `POST /csh/v1/hotels/{HotelId}/check/{checkNumber}`
  con `checkDetails.checkImage`. Debe validarse contra el ambiente OHIP no productivo asignado.

## Valores requeridos

No guardar secretos en `appsettings.json`. Configure los valores con variables de entorno o un
proveedor de secretos:

```powershell
$env:OperaCloud__UseMock = 'false'
$env:OperaCloud__GatewayUrl = ''
$env:OperaCloud__AppKey = ''
$env:OperaCloud__ClientId = ''
$env:OperaCloud__ClientSecret = ''
$env:OperaCloud__HotelId = ''
$env:OperaCloud__AttachmentUserName = ''
```

Para un ambiente Client Credentials:

```powershell
$env:OperaCloud__GrantType = 'client_credentials'
$env:OperaCloud__EnterpriseId = ''
$env:OperaCloud__Scope = 'urn:opc:hgbu:ws:__myscopes__'
```

Para un ambiente Resource Owner:

```powershell
$env:OperaCloud__GrantType = 'password'
$env:OperaCloud__Username = ''
$env:OperaCloud__Password = ''
```

`ExternalSystemCode` es opcional y evita que una escritura sea reenviada a la misma integración
cuando se usan eventos. `GatewayUrl`, flujo OAuth, `EnterpriseId`, credenciales y `AppKey` deben
copiarse del ambiente y aplicación correctos en el OHIP Developer Portal. `HotelId` lo proporciona
el hotel y debe enviarse tanto en la ruta como en `x-hotelid`. Para varios resorts configure el
diccionario `HotelIds`; el valor simple `HotelId` queda como fallback:

```json
{
  "OperaCloud": {
    "HotelId": "",
    "HotelIds": {
      "VILC": "HOTEL_OHIP_VILC",
      "VINV": "HOTEL_OHIP_VINV"
    }
  }
}
```

En variables de entorno: `OperaCloud__HotelIds__VILC=HOTEL_OHIP_VILC`. Si no existe mapeo ni
fallback, el cheque falla de forma controlada y no se marca procesado en la tabla financiera.

Los tokens se almacenan solo en memoria y se reutilizan hasta cinco minutos antes de expirar. La
aplicación nunca registra el token ni el client secret.
