<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="WebServis_TEST._Default" Async="true" %>

<%@ Register Assembly="DevExpress.Web.v17.1, Version=17.1.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web" TagPrefix="dx" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">

        <asp:Button ID="Button1" runat="server" Text="Button" />
        <br />
        <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>


        <dx:ASPxGridView ID="grdLog" runat="server" AutoGenerateColumns="true" ></dx:ASPxGridView>

    </form>
</body>
</html>
