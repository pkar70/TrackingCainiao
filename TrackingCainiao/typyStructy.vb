Imports Newtonsoft.Json


#Region "Cainaio"

Public Class JsonOneDetail
    Public Property desc As String      ' "Arrive at destination country"
    Public Property status As String    ' "ARRIVED_AT_DEST_COUNTRY"
    Public Property time As String      ' "2020-10-19 17:14:24"
    Public Property timeZone As String  ' "+2"
End Class

Public Class JsonSection
    Public Property countryName As String   ' "China" oraz "Poland"
    Public Property detailList As List(Of JsonOneDetail)    ' przy wysylanym: empty, przy Poland wypelniona
End Class

Public Class JsonCpEntry
    Public Property country As String '"Malaysia"
    Public Property cpCode As String '"CROSSBORDERSHUNYOU"
    Public Property cpName As String '"CROSSBORDERSHUNYOU"
End Class
Public Class JsonOneShipment
    Public Property allowRetry As Boolean   ' false
    Public Property bizType As String   ' "P2P"
    Public Property cachedTime As String ' "2020-10-21 19:57:58",
    Public Property destCountry As String
    ''Public Property destCpList As List(Of )": [],

    Public Property hasRefreshBtn As Boolean  ' true
    Public Property latestTrackingInfo As JsonOneDetail

    Public Property mailNo As String
    Public Property originCountry As String
    ''Public Property originCpList As List(Of )": [],
    Public Property section1 As JsonSection
    Public Property section2 As JsonSection

    Public Property shippingTime As Double ' dni?
    Public Property showEstimateTime As Boolean ' false
    Public Property status As String
    Public Property statusDesc As String
    Public Property success As Boolean  ' true
    Public Property syncQuery As Boolean    ' false

End Class

Public Class JsonWebResponse
    Public Property data As List(Of JsonOneShipment)
    Public Property success As Boolean
    Public Property timeSeconds As Double
End Class

Public Class JsonRefreshResponse
    Public Property allowRetry As Boolean ' :false,
    Public Property cachedTime As String ' :"2020-11-05 19:24:15",
    Public Property destCountry As String ' ":"Poland",
    'Public Property destCpList ':[],
    Public Property errorCode As String ':"REFRESH_NOT_MODIFIED",
    Public Property errorMsg As String ' "主动刷新未变更",
    'Public Property hasRefreshBtn As Boolean ' false,"
    Public Property mailNo As String ':"PL000009374753",
    'Public Property originCountry As String '":"China","
    ' Public Property originCpList As ":[],"
    'Public Property shippingTime As Double '":12.0,"
    'Public Property showEstimateTime As Boolean '":false,"
    Public Property status As String ' ":"ARRIVED_AT_DEST_COUNTRY","
    Public Property statusDesc As String '":"Your parcel has arrived in the country of destination.","
    Public Property success As Boolean '":false,"
    'Public Property syncQuery As Boolean '":false})

End Class

#End Region

#Region "Sypost"
Public Class JsonSyEvent
    Public Property content As String
    Public Property createTime As Long
    Public Property status As Integer
    Public Property timeZone As String

End Class

Public Class JsonSyOrigin
    Public Property items As List(Of JsonSyEvent)
End Class

Public Class JsonSyResult
    Public Property origin As JsonSyOrigin
End Class

Public Class JsonSyDataItem
    Public Property result As JsonSyResult
    Public Property orderNo As String   ' SYAET02761875
    Public Property lastContent As String   ' "HONG KONG, Departed HONG KONG Airport"
    Public Property lastUpdate As Long  ' 1606115517000
    Public Property dstCountry As String    ' PL
    Public Property days As Integer     '6
    Public Property displayStatus As Integer    ' 1
    Public Property has As Boolean      ' true
    Public Property lastStatus As Integer ' 106
End Class

Public Class JsonSyWebResponse
    Public Property data As List(Of JsonSyDataItem)
    Public Property message As String ' ok
    Public Property status As Integer ' 1
End Class

#End Region

#Region "UWP"

Public Class JednaPaczka
    Public Property sNazwa As String
    Public Property sNumer As String
    Public Property sLastDate As String
    Public Property sLastEvent As String
    Public Property JSONshipment As JsonOneShipment
    Public Property bDostalem As Boolean = False

End Class
#End Region
