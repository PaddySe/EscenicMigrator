<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EscenicImport.aspx.cs" Inherits="EscenicMigrator.EscenicImport" EnableViewState="false" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
	<head runat="server">
		<title>Escenic Import</title>
		<script type="text/javascript" src="//ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js"></script>
		<style type="text/css">
			* { font: 8pt Consolas; }
			label, legend { font: bold 110% Cambria; }
			table { border-spacing: 0; width: 100%; }
			table#articles, table#sections, table#media { display: none; }
			th { font: bold 100% Cambria; text-align: left; padding: 0 5px; }
			td { vertical-align: top; padding: 2px 5px; }
			tr { background-color: #fff; }
			table#articles tr:nth-child(4n+1), table#sections tr:nth-child(4n+1), table#media tr:nth-child(4n+1) { background-color: #eee; }
			table table tr:nth-child(odd) { background-color: #eee; }
			table.details th td:nth-child(0) { width: 150px; }
			fieldset { margin-bottom: 10px; }
			input { padding: 2px; }
			input[type=text] { width: 350px; }
			input[type=submit] { font: 110% Cambria; }
		</style>
	</head>
	<body>
		<form id="form1" runat="server">
			<div>
				<fieldset>
					<legend>Settings</legend>
					<asp:Label runat="server" AssociatedControlID="txtSourceDirectory" Text="Read files from directory:" />
					<asp:RequiredFieldValidator runat="server" ControlToValidate="txtSourceDirectory" ErrorMessage="*" />
					<asp:TextBox runat="server" ID="txtSourceDirectory" Text="C:\Temp\Norsk Ukeblad Import\norskukeblad" />
					<asp:Button runat="server" ID="btnParseFiles" OnClick="btnParseFiles_OnClick" Text="Parse" /><br/>
					<asp:Label runat="server" AssociatedControlID="txtUploadPath" Text="Upload path:" />
					<asp:RequiredFieldValidator runat="server" ControlToValidate="txtUploadPath" ErrorMessage="*" />
					<asp:TextBox runat="server" ID="txtUploadPath" Text="~/Upload/NorskUkeblad"/>
					<asp:Button runat="server" ID="btnImport" OnClick="btnImport_OnClick" Text="Import" />
					<asp:CustomValidator runat="server" ID="valDirectories" OnServerValidate="OnServerValidate" ErrorMessage="Import directory doesn't exist" />
				</fieldset>
				<fieldset>
					<legend>Articles (<asp:Literal runat="server" ID="litArticles" Text="0" />)</legend>
					<asp:Repeater runat="server" ID="rptArticles" EnableViewState="False">
						<HeaderTemplate>
							<table id="articles">
								<thead>
									<tr>
										<th></th>
										<th>Id</th>
										<th>Type</th>
										<th>State</th>
										<th>Publish Date</th>
										<th>Sections</th>
										<th>References</th>
										<th>Relations</th>
										<th>Field:Title</th>
									</tr>
								</thead>
								<tbody>
						</HeaderTemplate>
						<ItemTemplate>
							<tr>
								<td><a href="#" class="details">Fields</a></td>
								<td><%# Eval("Id") %></td>
								<td><%# Eval("Type") %></td>
								<td><%# Eval("State") %></td>
								<td><%# Eval("PublishDate") %></td>
								<td><%# Eval("Sections") %></td>
								<td><%# Eval("References.Files") %></td>
								<td><%# Eval("Relations") %></td>
								<td><%# Eval("Fields[TITLE]") %></td>
							</tr>
							<tr style="display: none;">
								<td></td>
								<td colspan="8">
									<table class="details">
										<thead>
											<tr>
												<th>Field name</th>
												<th>Field value</th>
											</tr>
										</thead>
										<tbody>
											<asp:Repeater runat="server" DataSource='<%# Eval("Fields") %>'>
												<ItemTemplate>
													<tr>
														<td><%# Eval("Key") %></td>
														<td><%# Server.HtmlEncode((string)Eval("Value")) %></td>
													</tr>
												</ItemTemplate>
											</asp:Repeater>
										</tbody>
									</table>
								</td>
							</tr>
						</ItemTemplate>
						<FooterTemplate>
							</tbody> </table>
						</FooterTemplate>
					</asp:Repeater>
				</fieldset>
				<fieldset>
					<legend>Sections (<asp:Literal runat="server" ID="litSections" Text="0" />)</legend>
					<asp:Repeater runat="server" ID="rptSections">
						<HeaderTemplate>
							<table id="sections">
								<thead>
									<tr>
										<th>Id</th>
										<th>Name</th>
										<th>Page reference</th>
									</tr>
								</thead>
								<tbody>
						</HeaderTemplate>
						<ItemTemplate>
							<tr>
								<td><%# Eval("Id") %></td>
								<td><%# Eval("Name") %></td>
								<td><%# Eval("PageReference") %></td>
							</tr>
						</ItemTemplate>
						<FooterTemplate>
							</tbody> </table>
						</FooterTemplate>
					</asp:Repeater>
				</fieldset>
				<fieldset>
					<legend>Media files (<asp:Literal runat="server" ID="litMedia" Text="0" />)</legend>
					<asp:Repeater runat="server" ID="rptMedia" EnableViewState="False">
						<HeaderTemplate>
							<table id="media">
								<thead>
									<tr>
										<th>Id</th>
										<th>Name</th>
										<th>Type</th>
										<th>AltText</th>
										<th>Description</th>
										<th>FileName</th>
									</tr>
								</thead>
								<tbody>
						</HeaderTemplate>
						<ItemTemplate>
							<tr>
								<td><%# Eval("Id") %></td>
								<td><%# Eval("Name") %></td>
								<td><%# Eval("Type")%></td>
								<td><%# Eval("AltText")%></td>
								<td><%# Eval("Description")%></td>
								<td><%# Eval("FileName")%></td>
							</tr>
						</ItemTemplate>
						<FooterTemplate>
							</tbody> </table>
						</FooterTemplate>
					</asp:Repeater>
				</fieldset>
				<fieldset>
					<legend>Log</legend>
					<asp:Literal runat="server" ID="litLog" />
				</fieldset>
			</div>
		</form>
		<script type="text/javascript">
			$(function () {
				$('a.details').click(function (event) {
					event.preventDefault();
					$(this).parent().parent().next().toggle();
				});

				$('#articles').siblings('legend').click(function (event) {
					event.preventDefault();
					$('#articles').toggle();
				});

				$('#sections').siblings('legend').click(function (event) {
					event.preventDefault();
					$('#sections').toggle();
				});

				$('#media').siblings('legend').click(function (event) {
					event.preventDefault();
					$('#media').toggle();
				});
			});
		</script>
	</body>
</html>
