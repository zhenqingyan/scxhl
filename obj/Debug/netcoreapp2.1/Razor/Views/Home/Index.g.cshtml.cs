#pragma checksum "C:\Yan\henglong\henglong.web\Views\Home\Index.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a00971425e459b17c28d31f06e3257ccda831532"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_Index), @"mvc.1.0.view", @"/Views/Home/Index.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Home/Index.cshtml", typeof(AspNetCore.Views_Home_Index))]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"a00971425e459b17c28d31f06e3257ccda831532", @"/Views/Home/Index.cshtml")]
    public class Views_Home_Index : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "C:\Yan\henglong\henglong.web\Views\Home\Index.cshtml"
  
    ViewData["Title"] = "Home Page";

#line default
#line hidden
            BeginContext(45, 239, true);
            WriteLiteral("<i-Layout id=\"app\">\r\n  <i-Content>\r\n    <Row>\r\n      <i-col span=\'12\'>\r\n        <Upload action=\"ExportFile\" accept=\'.jpg\'>\r\n          <i-button>上传图片</i-button>\r\n        </Upload>\r\n      </i-col>\r\n      <i-col span=\'12\'>\r\n        <i-button ");
            EndContext();
            BeginContext(285, 246, true);
            WriteLiteral("@click=\'QueryData\'>查询</i-button>\r\n      </i-col>\r\n    </Row>\r\n  </i-Content>\r\n    <i-Content>\r\n      <i-table :columns=\"tableData.columns\" :data=\"tableData.data\" :loading=\"tableData.loading\">\r\n\r\n      </i-table>\r\n    </i-Content>\r\n</i-Layout>\r\n\r\n");
            EndContext();
            DefineSection("Scripts", async() => {
                BeginContext(550, 2602, true);
                WriteLiteral(@"
  <script>
    $(function(){

      var vueInstance=function(){
        var init=function(){

          var vue=new Vue({
            el:""#app"",
            data:function(){
              let _self=this;
              return {
                  tableData:{
                    columns:[{
                        type: 'index',
                        width:60,
                        align: 'center'
                    },{
                        title: '唯一标识',
                        key: 'guid'
                    },{
                      title:'图片',
                      render:(h,params)=>{
                        return h('img',{
                          style:{
                            width:'100px',
                            height:'100px'
                          },
                          domProps:{
                            src:'GetImg?guid='+params.row.guid,
                          }
                        })
                      }
                   ");
                WriteLiteral(@" },{
                      title:'操作',
                      render:(h,params)=>{
                        let _self=this;
                        return h('i-switch', {
                          props:{
                            loading:false,
                            value:params.row.status
                          },
                          on:{
                            'on-change':(value)=>{
                              axios.post('Update',{
                                guid:params.row.guid,
                                status:value}).then(function(resp){
                                  _self.$Message.info(resp.data);
                                });
                            }
                          }
                        })
                      }
                    }],
                    data:[],
                    loading:false
                  }
              };
            },
            methods:{
                QueryData:function(){
   ");
                WriteLiteral(@"               let _self=this;
                  _self.tableData.loading=true;
                  axios.get('GetImgs').then(function(resp){
                    _self.tableData.data=resp.data;
                    _self.tableData.loading=false;
                  })
                }
            },
            mounted:function(){
              let _self=this;
              _self.QueryData();
            }
          })
        }
        return {
          Init:init
        }
      }();

      vueInstance.Init();
    })
  </script>
");
                EndContext();
            }
            );
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591