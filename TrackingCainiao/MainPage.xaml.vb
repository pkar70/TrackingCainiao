
Imports Microsoft.VisualBasic.CompilerServices
Imports System.Linq


Public NotInheritable Class MainPage
    Inherits Page


    Private Sub PokazItemy()
        uiItems.ItemsSource = From c In App.gItems Where c.bDostalem = False
        uiMsg.Text = (From c In App.gItems Where c.bDostalem = False).Count & " items"
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        GetAppVers(uiVers)
        ProgRingInit(True, False)

        GetSettingsBool(uiClockRead, "autoRead")

        If Await CanRegisterTriggersAsync() Then
            uiClockRead.IsChecked = IsTriggersRegistered("Sledzik")
        End If

        If App.gItems Is Nothing Then
            ' wczytanie tylko gdy jeszcze nie mamy tego wczytanego
            If Not Await App.LoadItemsAsync() Then Return
        End If

        PokazItemy()
    End Sub

    Private Sub uiRozwinAdd_Click(sender As Object, e As RoutedEventArgs) Handles uiRozwinAdd.Click
        uiAddForm.Visibility = Visibility.Visible
        uiRozwinAdd.Visibility = Visibility.Collapsed
    End Sub

    Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs) Handles uiAdd.Click

        If uiPaczkaNumer.Text.Length < 5 Then
            DialogBox("ERROR numer za krótki!")
            Return
        End If
        If uiPaczkaNazwa.Text.Length < 4 Then
            DialogBox("ERROR nazwa za krótka")
            Return
        End If

        For Each oItem As JednaPaczka In App.gItems
            If oItem.sNumer = uiPaczkaNumer.Text Then
                DialogBox("ten numer juz istnieje!")
                Return
            End If
        Next

        uiItems.ItemsSource = Nothing    ' jako pomysl na odswiezenie listy

        Dim oNew As JednaPaczka = New JednaPaczka
        oNew.sNazwa = uiPaczkaNazwa.Text
        oNew.sNumer = uiPaczkaNumer.Text

        App.gItems.Add(oNew)
        PokazItemy()

        App.SaveItemsAsync() ' nie czekamy - zdąży zapisać zanim kolejne coś się naciśnie...

        uiAddForm.Visibility = Visibility.Collapsed
        uiRozwinAdd.Visibility = Visibility.Visible
        uiPaczkaNazwa.Visibility = Visibility.Visible

    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        ProgRingShow(True)
        Dim iResult As Integer = Await App.GetWebData(True)
        iResult += Await App.GetPPWebData(True)
        ProgRingShow(False)

        If iResult > 0 Then Return   ' był error (-1) lub bez zmian (0)

        uiItems.ItemsSource = App.gItems
        App.SaveItemsAsync()

    End Sub

    Private Sub uiOpenExpl_Click(sender As Object, e As RoutedEventArgs)
        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Windows.System.Launcher.LaunchFolderAsync(oFold)
    End Sub
    Private Async Sub uiClockRead_Click(sender As Object, e As RoutedEventArgs)
        If Not Await CanRegisterTriggersAsync() Then Return

        If uiClockRead.IsChecked Then
            Await RegisterTriggers()
        Else
            UnregisterTriggers("Sledzik")
        End If
    End Sub


    Private Function GetLinkFor(sender As Object) As String
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        Dim sBaseUri As String = "https://global.cainiao.com/detail.htm?mailNoList="
        Return sBaseUri & oItem.sNumer
    End Function

    Private Sub uiCopyLinkThis_Click(sender As Object, e As RoutedEventArgs)
        Dim sUri As String = GetLinkFor(sender)
        ClipPut(sUri)
    End Sub
    Private Sub uiGoWebThis_Click(sender As Object, e As RoutedEventArgs)
        Dim sUri As String = GetLinkFor(sender)
        OpenBrowser(sUri)
    End Sub
    Private Sub uiGoSypostThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        Dim sUri As String = "https://www.sypost.net/search?orderNo=" & oItem.sNumer
        OpenBrowser(sUri)
    End Sub
    Private Sub uiGoPPWebThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        Dim sUri As String = "https://emonitoring.poczta-polska.pl/?numer=" & oItem.sNumer
        OpenBrowser(sUri)
    End Sub
    Private Async Sub uiRefreshThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        If oItem Is Nothing Then Return

        Dim sUri As String = "https://slw16.global.cainiao.com/trackRefreshRpc/refresh.json?mailNo=" & oItem.sNumer

        Dim sPage As String = Await HttpPageAsync(sUri, "refresh page", True)
        ' ({"allowRetry":false,"cachedTime":"2020-11-05 19:24:15","destCountry":"Poland","destCpList":[],"errorCode":"REFRESH_NOT_MODIFIED","errorMsg":"主动刷新未变更","hasRefreshBtn":false,"mailNo":"PL000009374753","originCountry":"China","originCpList":[],"shippingTime":12.0,"showEstimateTime":false,"status":"ARRIVED_AT_DEST_COUNTRY","statusDesc":"Your parcel has arrived in the country of destination.","success":false,"syncQuery":false})

        sPage = sPage.TrimStart("(")
        sPage = sPage.TrimEnd(")")

        Dim oJsonRefreshResponse As JsonRefreshResponse = Nothing
        Try
            oJsonRefreshResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(JsonRefreshResponse))
        Catch ex As Exception

        End Try
        If oJsonRefreshResponse Is Nothing Then
            DialogBox("ERROR converting JSON from web")
            Return
        End If

        If oJsonRefreshResponse.errorCode.ToUpper = "REFRESH_NOT_MODIFIED" Then
            DialogBox("nihil novi przesyłki " & oItem.sNazwa)
            Return
        End If

        DialogBox("zmiana przesyłki " & oItem.sNazwa & ", ale nie umiem obsłużyć :)")


    End Sub
    Private Sub uiShowDetailsThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        If oItem Is Nothing Then Return

        Dim detailList As List(Of JsonOneDetail) = oItem?.JSONshipment?.section2?.detailList
        If detailList Is Nothing Then
            DialogBox("detailList jest empty")
            Return
        End If

        uiDetailsItems.ItemsSource = detailList
        uiPaczkaName.Text = oItem.sNazwa & " (" & oItem.sNumer & ")"
        uiDetailsPaczki.Visibility = Visibility.Visible

        ' otwarcie strony, z której będzie back do tejże
        ' a na której będzie inny ListView. Albo dać tu ten inny ListView, np. na dolne pół strony, zwykle Hidden

    End Sub
    Private Sub uiDostalemThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = sender
        Dim oItem As JednaPaczka = oMFI.DataContext
        If oItem Is Nothing Then Return

        Dim sEndStatus As String = "odebrane (" & oMFI.Text & ")"

        For Each oPaczka As JednaPaczka In App.gItems
            If oPaczka.sNumer = oItem.sNumer Then
                oPaczka.bDostalem = True
                Dim detailList As List(Of JsonOneDetail) = oItem?.JSONshipment?.section2?.detailList
                If detailList IsNot Nothing Then
                    Dim oNew = New JsonOneDetail
                    oNew.desc = sEndStatus
                    oNew.status = "MAMTO"
                    oNew.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    detailList.Insert(0, oNew)
                End If
                oPaczka.sLastEvent = sEndStatus
                oPaczka.sLastDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                Exit For
            End If
        Next

        App.SaveItemsAsync()
        PokazItemy()

    End Sub

    Private Sub uiDetailsClose_Click(sender As Object, e As RoutedEventArgs)
        uiDetailsPaczki.Visibility = Visibility.Collapsed
    End Sub
    Private Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)
        Dim oATB As AppBarToggleButton = TryCast(sender, AppBarToggleButton)
        If oATB Is Nothing Then Return

        If oATB.IsChecked Then
            uiPaczkaNumer.Visibility = Visibility.Visible
            uiPaczkaNazwa.Visibility = Visibility.Collapsed
            uiAddForm.Visibility = Visibility.Visible
        Else
            uiPaczkaNumer.Visibility = Visibility.Visible
            uiPaczkaNazwa.Visibility = Visibility.Visible
            uiAddForm.Visibility = Visibility.Collapsed
        End If

        uiPaczkaNumer.Focus(FocusState.Keyboard)
    End Sub

    ' Private mInSearch As Boolean = False

    Private Sub uiPaczkaNumer_TextChanged(sender As Object, e As TextChangedEventArgs) Handles uiPaczkaNumer.TextChanged
        If uiPaczkaNazwa.Visibility = Visibility.Visible Then Return ' rozróżnienie między dodawaniem a szukaniem
        'If mInSearch Then Return ' wywołanie stąd (z uzupełniania)

        Dim sTerm As String = uiPaczkaNumer.Text.ToLower

        uiItems.ItemsSource = From c In App.gItems Where c.bDostalem = False And c.sNumer.ToLower.StartsWith(sTerm)

        Dim iCount As Integer = 0
        Dim sMore As String = ""
        For Each oItem As JednaPaczka In App.gItems
            Dim sNumer As String = oItem.sNumer.ToLower
            If Not oItem.bDostalem AndAlso sNumer.StartsWith(sTerm) Then
                If sMore = "" Then
                    If oItem.sNumer.Length > uiPaczkaNumer.Text.Length Then sMore = oItem.sNumer.Substring(0, uiPaczkaNumer.Text.Length + 1)
                End If
                iCount += 1
            End If
        Next

        Dim iCountMore As Integer = 0
        'Dim sMore1 As String = ""
        For Each oItem As JednaPaczka In App.gItems
            Dim sNumer As String = oItem.sNumer.ToLower
            If Not oItem.bDostalem AndAlso sNumer.StartsWith(sMore.ToLower) Then
                'If sMore1 = "" Then sMore = oItem.sNumer.Substring(0, sMore.Length + 1)
                iCountMore += 1
            End If
        Next

        If iCount = iCountMore Then
            uiPaczkaNumer.Text = sMore
            uiPaczkaNumer.SelectionLength = 0
            uiPaczkaNumer.SelectionStart = sMore.Length
        End If


        'mInSearch = True
        ' znajdz wszystkie oItem pasujace do tego ^mask
        ' ewentualnie przedluz
        'mInSearch = False

    End Sub

#Region "triggers"

    Public Async Function RegisterTriggers() As Task

        ' na pewno musza byc usuniete
        UnregisterTriggers("Sledzik")
        If Not Await CanRegisterTriggersAsync() Then Return

        RegisterTimerTrigger("SledzikTimer", GetSettingsInt("TimerInterval", 4 * 60))
        RegisterToastTrigger("SledzikToast")

    End Function


#End Region

End Class
