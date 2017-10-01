Imports System.Data.SqlClient
Imports System.IO
Imports System.Xml
Imports System.Xml.XPath
Imports System.Xml.Xsl
Imports WebServis_TEST.Uyumsoft
Public Enum FaturaTipi
    IHRACAT
    TEMELFATURA
    TICARIFATURA
End Enum

Public Class _Default

    Inherits System.Web.UI.Page
    Dim _username As String = "Uyumsoft"
    Dim _password As String = "Uyumsoft"
    Dim _serviceuri As String = "https://efatura-test.uyumsoft.com.tr/Services/Integration"
    Dim connectionString As String = "Server=.;trusted_connection=false;uid=sa;pwd=0000;database='BAHADIR'"

    Dim _bahadırUnvan As String = "Bahadır Tıbbi Aletler A.Ş"
    Dim _bahadırVD As String = "19 Mayıs"
    Dim _bahadırVKN As String = "9000068418"
    Dim _bahadırBinaNumarası As String = ""
    Dim _bahadırDaireNo As String = ""
    Dim _bahadırCaddeSokak As String = "Organize Sanayi"
    Dim _bahadırİlçe As String = "55300 Tekkeköy"
    Dim _bahadırUrl As String = "http://www.bahadir.com.tr"
    Dim _bahadırEposta As String = "info@bahadir.com.tr"
    Dim _bahadırŞehir As String = "Samsun"
    Dim _bahadırÜlke As String = "TÜRKİYE"
    Dim _bahadırTelefon As String = "+9(0362) 4317948"
    Dim _bahadırTelefax As String = "+9(0362) 4317949"

    Dim _totalLineExtentionAmount As Decimal = 0 'TOPLAM FATURA TUTARI
    Dim _totalTaxInclusiveAmount As Decimal = 0 'VERGİ MATRAHI 
    Dim _totalAllowanceCharge As Decimal = 0
    Dim _totalTaxExculisiveAmount As Decimal = 0

    Protected Async Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim _faturaNumarası = "BHDR0000486380" 'IHRACAT FATURA 1
        'Dim _faturaNumarası = "BHDR0000486093" 'IHRACAT FATURA 2
        'Dim _faturaNumarası = "BHDR0000486111" 'HIZMET FATURA
        'Dim _faturaNumarası = "BHDR0000486015" 'TİCARİ FATURA
        If IsPostBack = False Then
            Dim _fatura As Fatura = GetInvoiceDataFromERP(_faturaNumarası)
            Await FaturaGönder(_fatura)
        End If
    End Sub

    Public Async Function FaturaGönder(ByVal _fatura As Fatura) As Threading.Tasks.Task

        Dim client = CreateClient()

        Dim _invoice1 As InvoiceInfo = FaturaOlustur(_fatura)
        Dim _yanıt = Await client.SendInvoiceAsync(New InvoiceInfo() {_invoice1})

        Response.Clear()

        If _yanıt.IsSucceded Then
            Response.Write("İşlem Başarılı.<br/>")
            Response.Write("=>Fatura Numarası : " & _yanıt.Value(0).Number & "<br/>")
            Response.Write("=>Fatura ID : " & _yanıt.Value(0).Id & "<br/>")
        Else
            Response.Write("İşlem Başarısız :(.<br/>")
            Response.Write(_yanıt.Message)
        End If

        Dim _gibFaturaNo As String = _yanıt.Value(0).Number

        Dim Liste As String() = New String() {_yanıt.Value(0).Id.ToString()}

        Dim Sonuc = client.GetOutboxInvoiceStatusWithLogs(Liste)

        Response.Write("Status Code : " & Sonuc.Value(0).StatusCode.ToString() & "<br/>")
        Response.Write("Envelope Status Code : " & Sonuc.Value(0).EnvelopeStatusCode.ToString() & "<br/>")

        'Response.Write("Statü :." & Sonuc.Value(0). & "<br/>")
        TextBox1.Text = _yanıt.Value(0).Id

        WriteInvoiceToXml(_invoice1, _yanıt.Value(0).Number)
        'client.QueryInvoiceGtbResponses(New List(Of String){ _yanıt.Value(0).Number.ToString()})

    End Function

    Public Function FaturaOlustur(ByVal _fatura As Fatura) As InvoiceInfo

        Dim eArsivFaturasıMı As Boolean = IsEInvoiceUser(_fatura.CariHesap.vergino)
        If eArsivFaturasıMı Then
            eArsivFaturasıMı = False
        Else
            eArsivFaturasıMı = True
        End If

        Dim eArşivFaturasıKağıtmı As Boolean = False
        Dim _faturaNumarası As String = _fatura.ftrno
        Dim _ProfileID As String = _fatura.FaturaTipi.ToString()
        Dim _CopyIndicator As Boolean = False
        Dim _UUID As String = Guid.NewGuid().ToString()
        Dim _IssueDate As Date = Date.Now
        Dim _IssueTime As Date = DateTime.Now().ToLocalTime
        Dim _InvoiceTypeCode As String = "ISTISNA"
        Dim _dövizTipi As String = _fatura.doviz
        Dim _LineCountNumeric As Decimal = 1 ' BU ALAN KALEM SAYISINI İÇERİRMİŞ FAKAT KALEM SAYISI BİRDEN FAZLA OLSA BİLE BU ALANA 1 YAZILACAKMIŞ
        Dim _kdvOrani As Decimal = _fatura.ftrkdv

        Dim _invoice As New InvoiceType
        '###################################### GENEL FATURA BİLGİLERİ '######################################

        _invoice.ProfileID = New ProfileIDType With {.Value = _ProfileID}
        '_invoice.ID = New IDType With {.Value = _faturaNumarası} 'EĞER BU ALANI SET ETMEZSEN SİSTEM OTOMATİK BİR FATURA NUMARASI VERİRMİŞ
        _invoice.CopyIndicator = New CopyIndicatorType With {.Value = _CopyIndicator}
        _invoice.UUID = New UUIDType With {.Value = _UUID}
        _invoice.IssueDate = New IssueDateType With {.Value = _IssueDate}
        _invoice.IssueTime = New IssueTimeType With {.Value = _IssueTime}
        _invoice.InvoiceTypeCode = New InvoiceTypeCodeType With {.Value = _InvoiceTypeCode}

        'NOTLAR EKLEMEK İSTERSEK BU ALANI AKTİF EDECEĞİZ?
        '_invoice.Note = New NoteType() {New NoteType With {.Value = "NOT 1"}, New NoteType With {.Value = "NOT 2"}} 

        _invoice.DocumentCurrencyCode = New DocumentCurrencyCodeType With {.Value = _dövizTipi}
        _invoice.PricingCurrencyCode = New PricingCurrencyCodeType With {.Value = _dövizTipi}
        '_invoice.PricingExchangeRate = New ExchangeRateType With {.SourceCurrencyCode = "TRY"}
        _invoice.LineCountNumeric = New LineCountNumericType With {.Value = _LineCountNumeric}

        '###################################### GENEL FATURA BİLGİLERİ '######################################

        '######################################  İSKONTO  ######################################
        '_invoice.AllowanceCharge = New AllowanceChargeType() {New AllowanceChargeType With {.ChargeIndicator = New ChargeIndicatorType With {.Value = True}, .Amount = New AmountType2 With {.currencyID = "TRY", .Value = 5}, .AllowanceChargeReason = New AllowanceChargeReasonType With {.Value = "Bayi İskontosu"}}}

        '######################################  İRSALİYE  ######################################
        'İrsaliye bilgileri için bu eleman kullanılabilecektir. Birden fazla irsaliyeye ait bilgilerin girilmesi ve irsaliye belgesinin faturaya eklenmesinde bu eleman kullanılabilecektir.
        '_invoice.DespatchDocumentReference = New DocumentReferenceType() {New DocumentReferenceType With {.ID = New IDType With {.Value = "IRS0000000000001"}, .IssueDate = New IssueDateType With {.Value = DateTime.Now}, .DocumentType = New DocumentTypeType With {.Value = "Irsaliye"}}}

        '######################################  XSLT ŞABLONU  ######################################
        'XSLT FATURA ŞABLONU VARSA MEVCUT ŞABLON KULLANILACAK YOKSA YENİDEN TASARLANACAK
        _invoice.AdditionalDocumentReference = GetXsltDocument()

        '######################################  SİPARİŞ BİLGİLERİ  ######################################
        'EĞER SİPARİŞ BİLGİLERİNİ'DE KAYDETMEK İSTERSEM BU ALANI AKTİF EDERİM
        '_invoice.OrderReference = New OrderReferenceType With {.ID = New IDType With {.Value = "ORD00000001"}, .IssueDate = New IssueDateType With {.Value = DateTime.Now}}

        '######################################  BAHADIR BİLGİLERİ (FATURAYI KESEN TARAF)  ######################################
        _invoice.AccountingSupplierParty = New SupplierPartyType With {
                    .Party = New PartyType With
                    {
                        .PartyName = New PartyNameType With {.Name = New NameType1 With {.Value = _bahadırUnvan}},
                        .PartyIdentification = New PartyIdentificationType() {New PartyIdentificationType() With {.ID = New IDType With {.schemeID = "VKN", .Value = _bahadırVKN}}},
                        .PostalAddress = New AddressType With
                        {
                            .CityName = New CityNameType With {.Value = _bahadırŞehir},
                            .StreetName = New StreetNameType With {.Value = _bahadırCaddeSokak},
                            .Country = New CountryType With {.Name = New NameType1 With {.Value = _bahadırÜlke}},
                            .Room = New RoomType With {.Value = _bahadırDaireNo},
                            .BuildingNumber = New BuildingNumberType With {.Value = _bahadırBinaNumarası},
                            .CitySubdivisionName = New CitySubdivisionNameType With {.Value = _bahadırİlçe}
                        },
                        .WebsiteURI = New WebsiteURIType With {.Value = _bahadırUrl},
                        .Contact = New ContactType With {.Telephone = New TelephoneType With {.Value = _bahadırTelefon}, .Telefax = New TelefaxType With {.Value = _bahadırTelefax}, .ElectronicMail = New ElectronicMailType With {.Value = _bahadırEposta}},
                        .PartyTaxScheme = New PartyTaxSchemeType With {.TaxScheme = New TaxSchemeType With {.Name = New NameType1 With {.Value = _bahadırVD}}}
                    }
            }

        '######################################  ALICI BİLGİLERİ (FATURAYI ALAN TARAF)  ######################################
        '_invoice.AccountingCustomerParty = GetAccountingCustomer(_fatura) '( ProfileId alanı IHRACAT olan faturlar için AccountingCustomerParty alanı boş bırakılarak web servisimize gönderildiğinde sistem bu alanları otomatik olarakatayacaktır

        '_invoice.Delivery = New DeliveryType() {New DeliveryType With {.DeliveryParty = New PartyType With {}}}

        _invoice.InvoiceLine = SatırlarıOluştur(_fatura)

        _invoice.TaxTotal = New TaxTotalType() {New TaxTotalType With {
                                                 .TaxSubtotal = New TaxSubtotalType() {New TaxSubtotalType With {
                                                                                            .Percent = New PercentType1 With {
                                                                                                        .Value = Math.Round(Convert.ToDecimal(0), 2)},
                                                                                                                .TaxCategory = New TaxCategoryType With {
                                                                                                                                .TaxScheme = New TaxSchemeType With {
                                                                                                                                            .TaxTypeCode = New TaxTypeCodeType With {.Value = "0015"},
                                                                                                                                                            .Name = New NameType1 With {.Value = "KDV"}},
                                                                                                                                                                        .TaxExemptionReason = New TaxExemptionReasonType With {
                                                                                                                                                                                                          .Value = "11/1-a Mal ihracatı"},
                                                                                                                                                                                                          .TaxExemptionReasonCode = New TaxExemptionReasonCodeType With {.Value = "301"}},
                                                                                                                                                                                                          .TaxAmount = New TaxAmountType With {.Value = Math.Round(Convert.ToDecimal(0), 2), .currencyID = "TRY"}}},
                                                                                                                                                                                                          .TaxAmount = New TaxAmountType With {.Value = Math.Round(Convert.ToDecimal(0), 2), .currencyID = "TRY"}}}

        _invoice.LegalMonetaryTotal = New MonetaryTotalType With {
                                        .LineExtensionAmount = New LineExtensionAmountType With {.Value = _totalLineExtentionAmount, .currencyID = "TRY"},
                                        .TaxExclusiveAmount = New TaxExclusiveAmountType With {.Value = _totalTaxExculisiveAmount, .currencyID = "TRY"},
                                        .TaxInclusiveAmount = New TaxInclusiveAmountType With {.Value = _totalTaxInclusiveAmount, .currencyID = "TRY"},
                                        .AllowanceTotalAmount = New AllowanceTotalAmountType With {.Value = _totalAllowanceCharge, .currencyID = "TRY"},
                                        .PayableAmount = New PayableAmountType With {.Value = _totalTaxInclusiveAmount, .currencyID = "TRY"}
        }

        _invoice.BuyerCustomerParty = GetAlıcıYabancıFirma(_fatura)

        Dim _invoiceInfo As InvoiceInfo = New InvoiceInfo

        If eArsivFaturasıMı = True Then

            Dim _InvoiceDeliveryType As InvoiceDeliveryType
            If eArşivFaturasıKağıtmı = True Then
                _InvoiceDeliveryType = InvoiceDeliveryType.Paper
            Else
                _InvoiceDeliveryType = InvoiceDeliveryType.Electronic
            End If

            _invoiceInfo.EArchiveInvoiceInfo = New EArchiveInvoiceInformation With {.DeliveryType = _InvoiceDeliveryType}

        End If

        '_invoiceInfo.LocalDocumentId = _fatura.ftrno
        _invoiceInfo.Invoice = _invoice
        _invoiceInfo.TargetCustomer = New CustomerInfo With {.Title = "GÜMRÜK VE TİCARET BAKANLIĞI", .VknTckn = "1460415308", .Alias = "urn:mail:ihracatpk@gtb.gov.tr"}
        _invoiceInfo.Scenario = InvoiceScenarioChoosen.Automated
        _invoiceInfo.ExtraInformation = Nothing

        Dim mail1 As MailingInformation = New MailingInformation With {
                                .EnableNotification = True,
                                .Attachment = New MailAttachmentInformation With {.Xml = True, .Pdf = True},
                                .To = "musteri@mail.com",
                                .Subject = "...... no'lu e-fatuuranız"
        }
        Dim mail2 As MailingInformation = New MailingInformation With {
                                .EnableNotification = True,
                                .Attachment = New MailAttachmentInformation With {.Xml = True, .Pdf = True},
                                .To = "muhasebe@bahadir.com",
                                .Subject = "...... no'lu e-fatuura"
        }

        _invoiceInfo.Notification = New MailingInformation() {mail1, mail2}

        Return _invoiceInfo

    End Function

    Public Function GetXsltDocument() As DocumentReferenceType()
        'Xslt Resource'tan set edilsin
        Dim xsltDoc As DocumentReferenceType() = New DocumentReferenceType() {New DocumentReferenceType()}
        xsltDoc(0) = New DocumentReferenceType() With {.ID = New IDType With {.Value = New Guid().ToString()},
                    .IssueDate = New IssueDateType With {.Value = DateTime.Now},
                    .DocumentType = New DocumentTypeType With {.Value = "xls"},
                    .Attachment = New AttachmentType With
                    {
                        .EmbeddedDocumentBinaryObject = New EmbeddedDocumentBinaryObjectType With
                        {
                            .filename = "customxslt.xslt",
                            .encodingCode = "Base64",
                            .mimeCode = "applicationxml",
                            .format = "",
                            .characterSetCode = "UTF-8",
                            .Value = My.Computer.FileSystem.ReadAllBytes("d:\general.xslt")
                        }
                    }}

        Return xsltDoc

    End Function

    Public Function SatırlarıOluştur(ByVal _fatura As Fatura) As InvoiceLineType()
        Dim i As Integer = 0
        Dim lines As InvoiceLineType() = New InvoiceLineType(100) {}
        For Each faturaSatır As FaturaSatır In _fatura.FaturaSatırları

            Dim faturaSiraNo As Integer = i + 1
            Dim _birimFiyatı As Decimal = Convert.ToDecimal(faturaSatır.bfiyat.Replace(" ", ""))
            Dim _miktar As Decimal = Convert.ToDecimal(faturaSatır.mktr)
            Dim _tutar As Decimal = _birimFiyatı * _miktar
            Dim _ürünAdı As String = faturaSatır.acklm

            Dim _iskontoVarMi As Boolean = False 'bahadır db de ki fatura tablolarında iskonto ile ilgili alan göremedim
            Dim _iskontoOrani As Decimal = 0 'bahadır db de ki fatura tablolarında iskonto ile ilgili alan göremedim
            Dim _iskontoTutari As Decimal = 0 'bahadır db de ki fatura tablolarında iskonto ile ilgili alan göremedim

            Dim _dövizTipi As String = "TRY"
            Dim _ölçüBirimiKodu As String = "C62"
            Dim _kdvOranı As Decimal = 0
            Dim _vergiTürüKodu As String = "0015"
            Dim _vergiTürüadı As String = "KDV"
            Dim _vergiMuafiyetNedeni As String = "11/1-a Mal ihracatı"
            Dim _teslimŞartı As String = "CFR"
            Dim _kapCinsi As String = "CK"
            Dim _kapNo As String = "1"
            Dim _kapMiktarı As Decimal = 1
            Dim _shipmentId As Decimal = 1
            Dim _taşımaŞekliId As Decimal = 1
            Dim _gtipKodu As String = "901890840019"
            Dim _gideceğiÜlke As String = "RUSSIA"
            Dim _gideceğiBinaNo As String = "24"
            Dim _gideceğiKapıNo As String = "5"
            Dim _gideceğiIlçeSemtMahalle As String = ""
            Dim _gideceğiŞehir As String = "MOSCOW"
            Dim _gideceğiCaddeSokakBulvar As String = "ULITSYA TIMURA FRUNZE"
            dd
            _totalLineExtentionAmount = _totalLineExtentionAmount + _tutar
            _totalTaxInclusiveAmount = _totalLineExtentionAmount + _tutar
            _totalTaxExculisiveAmount = _totalLineExtentionAmount + _tutar
            _totalAllowanceCharge = 0


            lines(i) = New InvoiceLineType
            lines(i).ID = New IDType With {.Value = faturaSiraNo}
            lines(i).InvoicedQuantity = New InvoicedQuantityType With {.Value = Math.Round(Convert.ToDecimal(_miktar), 2), .unitCode = _ölçüBirimiKodu}
            lines(i).LineExtensionAmount = New LineExtensionAmountType With {.Value = Math.Round(Convert.ToDecimal(_tutar), 2), .currencyID = _dövizTipi}
            lines(i).TaxTotal = New TaxTotalType With {
                .TaxSubtotal = New TaxSubtotalType() {New TaxSubtotalType With {
                        .Percent = New PercentType1 With {.Value = Math.Round(Convert.ToDecimal(_kdvOranı), 2)},
                        .TaxCategory = New TaxCategoryType With {
                            .TaxScheme = New TaxSchemeType With {
                                .TaxTypeCode = New TaxTypeCodeType With {.Value = _vergiTürüKodu}, .Name = New NameType1 With {.Value = _vergiTürüadı}},
                            .TaxExemptionReason = New TaxExemptionReasonType With {.Value = _vergiMuafiyetNedeni}},
                        .TaxAmount = New TaxAmountType With {.Value = Math.Round(Convert.ToDecimal(_kdvOranı), 2), .currencyID = _dövizTipi}}
                },
                .TaxAmount = New TaxAmountType With {.Value = Math.Round(Convert.ToDecimal(_kdvOranı), 2), .currencyID = _dövizTipi}
            }
            lines(i).Item = New ItemType With {
                            .Name = New NameType1 With {.Value = _ürünAdı}
            }
            lines(i).Price = New PriceType With {
                .PriceAmount = New PriceAmountType With {.Value = _birimFiyatı, .currencyID = _dövizTipi}
            }

            If _fatura.FaturaTipi = FaturaTipi.IHRACAT Then
                lines(i).Delivery = New DeliveryType() {
                New DeliveryType With {
                    .DeliveryTerms = New DeliveryTermsType() {
                                            New DeliveryTermsType With {
                                                           .ID = New IDType With {.schemeID = "INCOTERMS", .Value = _teslimŞartı}}},
                    .Shipment = New ShipmentType With {
                                    .ID = New IDType With {.Value = _shipmentId},
                                    .TransportHandlingUnit = New TransportHandlingUnitType() {
                                                                        New TransportHandlingUnitType With {
                                                                            .ActualPackage = New PackageType() {
                                                                                                    New PackageType With {
                                                                                                        .PackagingTypeCode = New PackagingTypeCodeType With {
                                                                                                                                    .Value = _kapCinsi},
                                                                                                                                    .ID = New IDType With {.Value = _kapNo},
                                                                                                                                    .Quantity = New QuantityType2 With {.Value = _kapMiktarı, .unitCode = _kapCinsi}
                                                                                                    }
                                                                            }
                                                                        }
                                    },
                                    .ShipmentStage = New ShipmentStageType() {
                                        New ShipmentStageType With {.TransportModeCode = New TransportModeCodeType With {.Value = _taşımaŞekliId}}
                                    },
                                    .GoodsItem = New GoodsItemType() {
                                        New GoodsItemType With {
                                            .DeclaredCustomsValueAmount = New DeclaredCustomsValueAmountType With {.Value = 15, .currencyID = _dövizTipi},
                                            .FreeOnBoardValueAmount = New FreeOnBoardValueAmountType With {.Value = 12, .currencyID = _dövizTipi},
                                            .RequiredCustomsID = New RequiredCustomsIDType With {.Value = _gtipKodu}
                                        }
                                    }
                    },
                    .DeliveryAddress = New AddressType With {
                            .CityName = New CityNameType With {.Value = _gideceğiŞehir},
                            .BuildingName = New BuildingNameType With {.Value = _gideceğiBinaNo},
                            .BuildingNumber = New BuildingNumberType With {.Value = _gideceğiKapıNo},
                            .Country = New CountryType With {.Name = New NameType1 With {.Value = _gideceğiÜlke}},
                            .StreetName = New StreetNameType With {.Value = _gideceğiCaddeSokakBulvar},
                            .CitySubdivisionName = New CitySubdivisionNameType With {.Value = _gideceğiIlçeSemtMahalle}
                    }
                }
            }
            End If

            i = i + 1

        Next
        Return lines
    End Function

    Private Function GetSatırNo(Sayi As Integer) As String

        Dim Sonuc As String = ""
        Dim Yıl As Integer = DateTime.Now().Year

        If Sayi < 10 Then
            Sonuc = "BHD" & Yıl & "00000000" & Sayi.ToString()
        ElseIf (Sayi >= 10 & Sayi < 100) Then
            Sonuc = "BHD" & Yıl & "0000000" & Sayi.ToString()
        ElseIf (Sayi >= 100 & Sayi < 1000) Then
            Sonuc = "BHD" & Yıl & "000000" & Sayi.ToString()
        ElseIf (Sayi >= 1000 & Sayi < 10000) Then
            Sonuc = "BHD" & Yıl & "00000" & Sayi.ToString()
        ElseIf (Sayi >= 10000 & Sayi < 100000) Then
            Sonuc = "BHD" & Yıl & "0000" & Sayi.ToString()
        ElseIf (Sayi >= 100000 & Sayi < 1000000) Then
            Sonuc = "BHD" & Yıl & "000" & Sayi.ToString()
        End If

        Return Sonuc

    End Function

    Public Function GetAccountingCustomer(ByVal _fatura As Fatura) As CustomerPartyType

        Dim müşteri As CustomerPartyType

        Dim _aliciFirmaÜnvanı As String = _fatura.CariHesap.musteri
        Dim _aliciFirmaVergiDairesi As String = _fatura.CariHesap.vergida
        Dim _aliciFirmaVKNO As String = _fatura.CariHesap.vergino
        Dim _aliciFirmaŞehir As String = ""
        Dim _aliciFirmaCaddeSokak As String = ""
        Dim _aliciFirmaÜlke As String = ""
        Dim _aliciFirmaKapıNo As String = ""
        Dim _aliciFirmaİlçe As String = ""
        Dim _aliciFirmaFaxNo As String = _fatura.CariHesap.telfax
        Dim _aliciFirmaEpostaAdresi As String = _fatura.CariHesap.email
        Dim _aliciFirmaTelefonNo As String = ""
        Dim _aliciFirmaWebSiteAdresi As String = ""
        Dim _aliciFirmaYetkiliAdi As String = ""
        Dim _aliciFirmaYetkiliSoyadi As String = ""

        Dim _schemeID As String = "PARTYTYPE"

        If _fatura.FaturaTipi.ToString() = "IHRACAT" Then

            müşteri = New CustomerPartyType With {
                    .Party = New PartyType With {
                        .PartyName = New PartyNameType With {.Name = New NameType1 With {.Value = "GÜMRÜK VE TİCARET BAKANLIĞI BİLGİ İŞLEM DAİRESİ BAŞKANLIĞI"}},
                        .PartyIdentification = New PartyIdentificationType() {New PartyIdentificationType With {.ID = New IDType With {.Value = "1460415308", .schemeID = "VKN"}}},
                        .PostalAddress = New AddressType With {
                            .CityName = New CityNameType With {.Value = "Ankara"},
                            .StreetName = New StreetNameType With {.Value = ">Üniversiteler Mahallesi Dumlupınar Bulvar"},
                            .Country = New CountryType With {.Name = New NameType1 With {.Value = "Türkiye"}},
                            .BuildingNumber = New BuildingNumberType With {.Value = "151"},
                            .CitySubdivisionName = New CitySubdivisionNameType With {.Value = "Çankaya"}
                        }
                    }
            }
        Else
            müşteri = New CustomerPartyType With {
                   .Party = New PartyType With {
                       .PartyName = New PartyNameType With {.Name = New NameType1 With {.Value = _aliciFirmaÜnvanı}},
                       .PartyIdentification = New PartyIdentificationType() {New PartyIdentificationType With {.ID = New IDType With {.Value = _aliciFirmaVKNO, .schemeID = _schemeID}}},
                       .PostalAddress = New AddressType With {
                           .CityName = New CityNameType With {.Value = _aliciFirmaŞehir},
                           .StreetName = New StreetNameType With {.Value = _aliciFirmaCaddeSokak},
                           .Country = New CountryType With {.Name = New NameType1 With {.Value = _aliciFirmaÜlke}},
                           .Room = New RoomType With {.Value = _aliciFirmaKapıNo},
                           .BuildingNumber = New BuildingNumberType With {.Value = _aliciFirmaKapıNo},
                           .CitySubdivisionName = New CitySubdivisionNameType With {.Value = _aliciFirmaİlçe}
                       },
                       .Contact = New ContactType With {.Telefax = New TelefaxType With {.Value = _aliciFirmaFaxNo}, .ElectronicMail = New ElectronicMailType With {.Value = _aliciFirmaEpostaAdresi}, .Telephone = New TelephoneType With {.Value = _aliciFirmaTelefonNo}},
                       .WebsiteURI = New WebsiteURIType With {.Value = _aliciFirmaWebSiteAdresi},
                       .PartyTaxScheme = New PartyTaxSchemeType With {.TaxScheme = New TaxSchemeType With {.Name = New NameType1 With {.Value = _aliciFirmaVergiDairesi}}},
                       .Person = New PersonType With {.FirstName = New FirstNameType With {.Value = _aliciFirmaYetkiliAdi}, .FamilyName = New FamilyNameType With {.Value = _aliciFirmaYetkiliSoyadi}}
                   }
           }
        End If

        Return müşteri

    End Function

    Public Function GetAlıcıYabancıFirma(ByVal _fatura As Fatura) As CustomerPartyType

        Dim customer As CustomerPartyType
        Dim _alıcıÜnvan As String = _fatura.CariHesap.musteri
        Dim _alıcıVKN As String = _fatura.CariHesap.vergino
        Dim _aliciÜlke As String = _fatura.CariHesap.ulke
        Dim _aliciŞehir As String = _fatura.CariHesap.sehir
        Dim _aliciİlçeAdı As String = _fatura.CariHesap.ilce
        Dim _aliciCaddeSokak As String = _fatura.CariHesap.caddesokak
        Dim _alıcıİçKapıNo As String = _fatura.CariHesap.ickapino
        Dim _alıcıDışKapıNo As String = _fatura.CariHesap.diskapino

        If _fatura.FaturaTipi.ToString() = "IHRACAT" Then

            customer = New CustomerPartyType With {
                    .Party = New PartyType With {
                        .PartyName = New PartyNameType With {.Name = New NameType1 With {.Value = _alıcıÜnvan}},
                        .PartyIdentification = New PartyIdentificationType() {New PartyIdentificationType With {.ID = New IDType With {.schemeID = "PARTYTYPE", .Value = "EXPORT"}}},
                        .PostalAddress = New AddressType With {
                            .CityName = New CityNameType With {.Value = _aliciŞehir},
                            .StreetName = New StreetNameType With {.Value = _aliciCaddeSokak},
                            .Country = New CountryType With {.Name = New NameType1 With {.Value = _aliciÜlke}},
                            .Room = New RoomType With {.Value = _alıcıİçKapıNo},
                            .BuildingNumber = New BuildingNumberType With {.Value = _alıcıDışKapıNo},
                            .CitySubdivisionName = New CitySubdivisionNameType With {.Value = _aliciİlçeAdı}
                        },
                        .PartyLegalEntity = New PartyLegalEntityType() {New PartyLegalEntityType With {
                                                                                .RegistrationName = New RegistrationNameType With {
                                                                                                                .Value = _alıcıÜnvan},
                                                                                                                .CompanyID = New CompanyIDType With {.Value = _alıcıVKN}}}
                    }
                }
            Return customer
        Else
            Return Nothing
        End If
    End Function

    Public Function IsEInvoiceUser(ByVal VKN As String) As Boolean
        Dim client = CreateClient()
        Dim Response = client.IsEInvoiceUser(VKN, "")
        Return Response.Value
    End Function

    Public Function CreateClient() As IntegrationClient
        Dim client As New IntegrationClient()
        client.Endpoint.Address = New System.ServiceModel.EndpointAddress(_serviceuri)
        client.ClientCredentials.UserName.UserName = _username
        client.ClientCredentials.UserName.Password = _password
        Return client
    End Function

    Public Function WriteInvoiceToXml(ByVal _invoice As InvoiceInfo, ByVal _invoiceNumber As String)
        Dim xmlFilePath As String = "d:\" & _invoiceNumber & ".xml"
        Dim serializer As New System.Xml.Serialization.XmlSerializer(_invoice.GetType)
        Using file As System.IO.FileStream = System.IO.File.Open(xmlFilePath, IO.FileMode.OpenOrCreate, IO.FileAccess.Write)
            serializer.Serialize(file, _invoice)
        End Using
    End Function

    Public Function GetInvoiceDataFromERP(ByVal _faturaNumarası As String) As Fatura

        Dim cnnFatura As SqlConnection = New SqlConnection(connectionString)
        Dim sorgu As String = "select *FROM MuhasebeFatura WHERE ftrno ='" & _faturaNumarası & "'"
        Dim istek As SqlCommand = New SqlCommand(sorgu, cnnFatura)
        cnnFatura.Open()
        Dim drFatura As SqlDataReader = istek.ExecuteReader()
        If drFatura.Read() Then

            Dim _fatura As Fatura = New Fatura() With {
                .ftrno = Trim(drFatura("ftrno").ToString()),
                .tarih = Trim(drFatura("tarih").ToString()),
                .hesapkodu = Trim(drFatura("hesapkodu").ToString()),
                .ftrkur = Trim(drFatura("ftrkur").ToString()),
                .musteri = Trim(drFatura("musteri").ToString()),
                .aciklama1 = Trim(drFatura("aciklama1").ToString()),
                .aciklama2 = Trim(drFatura("aciklama2").ToString()),
                .ftrtur = Trim(drFatura("ftrtur").ToString()),
                .ftrislem = Trim(drFatura("ftrislem").ToString()),
                .ftrdurum = Trim(drFatura("ftrdurum").ToString()),
                .ftrtermins = Trim(drFatura("ftrtermins").ToString()),
                .ftrtermint = Trim(drFatura("ftrtermint").ToString()),
                .ftrisk = Trim(drFatura("ftrisk").ToString()),
                .ftrkdv = Trim(drFatura("ftrkdv").ToString()),
                .ftrtutar = Trim(drFatura("ftrtutar").ToString()),
                .nakliye = Trim(drFatura("nakliye").ToString()),
                .sigorta = Trim(drFatura("sigorta").ToString()),
                .ambalaj = Trim(drFatura("ambalaj").ToString()),
                .netagirlik = Trim(drFatura("netagirlik").ToString()),
                .kolagirlik = Trim(drFatura("kolagirlik").ToString()),
                .kolsayisi = Trim(drFatura("kolsayisi").ToString()),
                .urunsayisi = Trim(drFatura("urunsayisi").ToString()),
                .doviz = Trim(drFatura("doviz").ToString()),
                .durum = Trim(drFatura("durum").ToString()),
                .teslimsarti = Trim(drFatura("teslimsarti").ToString()),
                .tasimasekli = Trim(drFatura("tasimasekli").ToString())
            }


            Select Case Trim(drFatura("ftrtur"))
                Case "0"
                    _fatura.FaturaTipi = FaturaTipi.TICARIFATURA
                Case "1"
                    _fatura.FaturaTipi = FaturaTipi.TEMELFATURA
                Case "2"
                    _fatura.FaturaTipi = FaturaTipi.TEMELFATURA
                Case "3"
                    _fatura.FaturaTipi = FaturaTipi.TEMELFATURA
                Case "4"
                    _fatura.FaturaTipi = FaturaTipi.TEMELFATURA
                Case "5"
                    _fatura.FaturaTipi = FaturaTipi.IHRACAT
            End Select



            Dim cnnFaturaSatir As SqlConnection = New SqlConnection(connectionString)
            sorgu = "select *FROM MuhasebeSatir WHERE stftrno ='" & _fatura.ftrno & "'"
            istek = New SqlCommand(sorgu, cnnFaturaSatir)
            cnnFaturaSatir.Open()
            Dim drFaturaSatır As SqlDataReader = istek.ExecuteReader()

            While drFaturaSatır.Read()
                _fatura.FaturaSatırları.Add(New FaturaSatır With {
                                .datano = drFaturaSatır("datano"),
                               .stprfno = Trim(drFaturaSatır("stprfno").ToString()),
                               .stsprno = Trim(drFaturaSatır("stsprno").ToString()),
                               .stirsno = Trim(drFaturaSatır("stirsno").ToString()),
                               .stftrno = Trim(drFaturaSatır("stftrno").ToString()),
                               .tarih = drFaturaSatır("tarih"),
                               .hesapkodu = Trim(drFaturaSatır("hesapkodu").ToString()),
                               .srno = Trim(drFaturaSatır("srno").ToString()),
                               .urnkd = Trim(drFaturaSatır("urnkd").ToString()),
                               .barkod = Trim(drFaturaSatır("barkod").ToString()),
                               .acklm = Trim(drFaturaSatır("acklm").ToString()),
                               .mktr = drFaturaSatır("mktr"),
                               .birm = Trim(drFaturaSatır("birm").ToString()),
                               .bfiyat = Trim(drFaturaSatır("bfiyat").ToString()),
                               .tutar = Trim(drFaturaSatır("tutar").ToString()),
                               .islm = Trim(drFaturaSatır("islm").ToString()),
                               .drm = Trim(drFaturaSatır("drm").ToString())
                })
            End While
            cnnFaturaSatir.Close()

            Dim cnnCariHesap As SqlConnection = New SqlConnection(connectionString)
            sorgu = "select *FROM CariHesapBlg WHERE chhesapkodu ='" & _fatura.hesapkodu & "'"
            istek = New SqlCommand(sorgu, cnnCariHesap)
            cnnCariHesap.Open()
            Dim drCariHesap As SqlDataReader = istek.ExecuteReader()

            If drCariHesap.Read() Then
                _fatura.CariHesap = New CariHesap With {
                            .chhesapkodu = Trim(drCariHesap("chhesapkodu").ToString()),
                            .krtarih = Trim(drCariHesap("krtarih").ToString()),
                            .musteri = Trim(drCariHesap("musteri").ToString()),
                            .bilginotu = Trim(drCariHesap("bilginotu").ToString()),
                            .adresa = Trim(drCariHesap("adresa").ToString()),
                            .adresb = Trim(drCariHesap("adresb").ToString()),
                            .vergida = Trim(drCariHesap("vergida").ToString()),
                            .vergino = Trim(drCariHesap("vergino").ToString()),
                            .telfax = Trim(drCariHesap("telfax").ToString()),
                            .email = Trim(drCariHesap("email").ToString()),
                            .kredi = Trim(drCariHesap("kredi").ToString()),
                            .borc = Trim(drCariHesap("borc").ToString()),
                            .alacak = Trim(drCariHesap("alacak").ToString()),
                            .indirim = Trim(drCariHesap("indirim").ToString()),
                            .tipi = Trim(drCariHesap("tipi").ToString())
                }
            Else

                Dim cnnCariHesapMuhBlg As SqlConnection = New SqlConnection(connectionString)
                sorgu = "select *FROM CariHesapMuhBlg WHERE chhesapkodu ='BHDR" & _fatura.hesapkodu & "'"
                istek = New SqlCommand(sorgu, cnnCariHesapMuhBlg)
                cnnCariHesapMuhBlg.Open()
                Dim drCariHesapMuhBlg As SqlDataReader = istek.ExecuteReader()
                If drCariHesapMuhBlg.Read() Then
                    _fatura.CariHesap = New CariHesap With {
                            .chhesapkodu = Trim(drCariHesapMuhBlg("chhesapkodu").ToString()),
                            .krtarih = Trim(drCariHesapMuhBlg("krtarih").ToString()),
                            .musteri = Trim(drCariHesapMuhBlg("musteri").ToString()),
                            .bilginotu = Trim(drCariHesapMuhBlg("bilginotu").ToString()),
                            .adresa = Trim(drCariHesapMuhBlg("adresa").ToString()),
                            .adresb = Trim(drCariHesapMuhBlg("adresb").ToString()),
                            .vergida = Trim(drCariHesapMuhBlg("vergida").ToString()),
                            .vergino = Trim(drCariHesapMuhBlg("vergino").ToString()),
                            .telfax = Trim(drCariHesapMuhBlg("telfax").ToString()),
                            .email = Trim(drCariHesapMuhBlg("email").ToString()),
                            .kredi = Trim(drCariHesapMuhBlg("kredi").ToString()),
                            .borc = Trim(drCariHesapMuhBlg("borc").ToString()),
                            .alacak = Trim(drCariHesapMuhBlg("alacak").ToString()),
                            .indirim = Trim(drCariHesapMuhBlg("indirim").ToString()),
                            .tipi = Trim(drCariHesapMuhBlg("tipi").ToString())
                    }
                End If

                cnnCariHesapMuhBlg.Close()

            End If
            cnnCariHesap.Close()
            Return _fatura
        Else
            Return Nothing
        End If
        cnnFatura.Close()
    End Function

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim client = CreateClient()
        Dim faturaId = TextBox1.Text
        Dim sonuc = client.GetOutboxInvoiceStatusWithLogs(New String() {faturaId}).Value(0).Logs
        grdLog.DataSource = sonuc
        grdLog.DataBind()
        'Dim sonuc = client.GetOutboxInvoice(faturaId)


        'Dim xsltran As New XslCompiledTransform()
        'Dim doc As New XPathDocument("d:\XXX.xml")
        'Dim xslDoc As New XPathDocument("d:\XXX.xslt")
        'Dim argLst As New XsltArgumentList()
        'xsltran.Load(xslDoc)
        'xsltran.Transform(doc, argLst, Response.Output)


    End Sub

End Class

Public Class Fatura

    Property FaturaTipi As FaturaTipi
    Property ftrno As String
    Property tarih As String
    Property hesapkodu As String
    Property ftrkur As String
    Property musteri As String
    Property aciklama1 As String
    Property aciklama2 As String
    Property ftrtur As String
    Property ftrislem As String
    Property ftrdurum As String
    Property ftrtermins As String
    Property ftrtermint As String
    Property ftrisk As String
    Property ftrkdv As String
    Property ftrtutar As String
    Property nakliye As String
    Property sigorta As String
    Property ambalaj As String
    Property netagirlik As String
    Property kolagirlik As String
    Property kolsayisi As String
    Property urunsayisi As String
    Property doviz As String
    Property durum As String
    Property teslimsarti As String
    Property tasimasekli As String
    Property CariHesap As CariHesap = New CariHesap
    Property FaturaSatırları As New List(Of FaturaSatır)

End Class

Public Class FaturaSatır
    Property datano As Integer
    Property stprfno As String
    Property stsprno As String
    Property stirsno As String
    Property stftrno As String
    Property tarih As DateTime
    Property hesapkodu As String
    Property srno As String
    Property urnkd As String
    Property barkod As String
    Property acklm As String
    Property mktr As Integer
    Property birm As String
    Property bfiyat As String
    Property tutar As String
    Property islm As String
    Property drm As String

End Class

Public Class CariHesap
    Property chhesapkodu As String
    Property krtarih As String
    Property musteri As String
    Property bilginotu As String
    Property adresa As String
    Property adresb As String
    Property vergida As String
    Property vergino As String
    Property telfax As String
    Property email As String
    Property kredi As String
    Property borc As String
    Property alacak As String
    Property indirim As String
    Property tipi As String


    Property diskapino As String
    Property ickapino As String
    Property caddesokak As String
    Property ilce As String
    Property sehir As String
    Property ulke As String


End Class

