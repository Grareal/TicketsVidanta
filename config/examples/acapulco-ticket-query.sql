SELECT
    CONVERT(nvarchar(80), hc.num_hab) AS Room,
    CONVERT(nvarchar(80), hc.caja) AS PointOfSale,
    CONVERT(nvarchar(80), hc.folio) AS CheckNumber,
    hc.fec_che AS BusinessDate,
    hc.hra_alta AS Time,
    hc.sub_total AS Subtotal,
    hc.tip AS Tip,
    hc.impuesto AS Tax,
    hc.total AS Total,
    CONVERT(nvarchar(80), ayb.codigo) AS ItemDescription,
    CAST(1 AS decimal(19,4)) AS ItemQuantity,
    hm.importe AS ItemAmount
FROM dbo.hotche AS hc
INNER JOIN dbo.hotcom AS hm
    ON RTRIM(CONVERT(nvarchar(80), hc.caja)) = RTRIM(CONVERT(nvarchar(80), hm.caja))
   AND RTRIM(CONVERT(nvarchar(80), hc.turno)) = RTRIM(CONVERT(nvarchar(80), hm.turno))
   AND RTRIM(CONVERT(nvarchar(80), hc.folio)) = RTRIM(CONVERT(nvarchar(80), hm.folio))
LEFT JOIN dbo.hotayb AS ayb
    ON RTRIM(CONVERT(nvarchar(80), ayb.codigo)) = RTRIM(CONVERT(nvarchar(80), hm.platillo))
WHERE LTRIM(RTRIM(CONVERT(nvarchar(256), hc.cargar_a))) = @ReservationId
  AND LTRIM(RTRIM(CONVERT(nvarchar(80), hc.folio))) = @CheckNumber
