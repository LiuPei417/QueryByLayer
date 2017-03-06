using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using System.Diagnostics;
using ESRI.ArcGIS.Geometry;

namespace QueryByLayer
{
    public sealed partial class MainForm : Form
    {
        #region class private members
        private IMapControl3 m_mapControl = null;
        private string m_mapDocumentName = string.Empty;
        string mxdPath = Application.StartupPath + "\\test.mxd";
        #endregion

        #region class constructor
        public MainForm()
        {
            InitializeComponent();
        }
        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            //get the MapControl
            m_mapControl = (IMapControl3)axMapControl1.Object;

            //disable the Save menu (since there is no document yet)
            menuSaveDoc.Enabled = false;

            if(m_mapControl.CheckMxFile(mxdPath))
            {
                m_mapControl.LoadMxFile(mxdPath);
            }

        }

        #region Main Menu event handlers
        private void menuNewDoc_Click(object sender, EventArgs e)
        {
            //execute New Document command
            ICommand command = new CreateNewDocument();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuOpenDoc_Click(object sender, EventArgs e)
        {
            //execute Open Document command
            ICommand command = new ControlsOpenDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuSaveDoc_Click(object sender, EventArgs e)
        {
            //execute Save Document command
            if (m_mapControl.CheckMxFile(m_mapDocumentName))
            {
                //create a new instance of a MapDocument
                IMapDocument mapDoc = new MapDocumentClass();
                mapDoc.Open(m_mapDocumentName, string.Empty);

                //Make sure that the MapDocument is not readonly
                if (mapDoc.get_IsReadOnly(m_mapDocumentName))
                {
                    MessageBox.Show("Map document is read only!");
                    mapDoc.Close();
                    return;
                }

                //Replace its contents with the current map
                mapDoc.ReplaceContents((IMxdContents)m_mapControl.Map);

                //save the MapDocument in order to persist it
                mapDoc.Save(mapDoc.UsesRelativePaths, false);

                //close the MapDocument
                mapDoc.Close();
            }
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            //execute SaveAs Document command
            ICommand command = new ControlsSaveAsDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            //exit the application
            Application.Exit();
        }
        #endregion

        //listen to MapReplaced evant in order to update the statusbar and the Save menu
        private void axMapControl1_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            //get the current document name from the MapControl
            m_mapDocumentName = m_mapControl.DocumentFilename;

            //if there is no MapDocument, diable the Save menu and clear the statusbar
            if (m_mapDocumentName == string.Empty)
            {
                menuSaveDoc.Enabled = false;
                statusBarXY.Text = string.Empty;
            }
            else
            {
                //enable the Save manu and write the doc name to the statusbar
                menuSaveDoc.Enabled = true;
                statusBarXY.Text = System.IO.Path.GetFileName(m_mapDocumentName);
            }
        }

        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            statusBarXY.Text = string.Format("{0}, {1}  {2}", e.mapX.ToString("#######.##"), e.mapY.ToString("#######.##"), axMapControl1.MapUnits.ToString().Substring(4));
        }

        //���ܣ���ѯ��Ҫ���������а�������Ҫ�����ڲ��ĵ�Ҫ��
        private void iSpatialFilterOneByOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch myWatch = Stopwatch.StartNew();
            //��MapControl�л�ȡ�ĵ�ͼ��
            IFeatureLayer pointFeatureLayer = axMapControl1.get_Layer(0) as IFeatureLayer;
            IFeatureSelection pointFeatureSelection = pointFeatureLayer as IFeatureSelection;
            //��MapControl�л�ȡ����ͼ��
            IFeatureLayer polygonFeatureLayer = axMapControl1.get_Layer(1) as IFeatureLayer;
            //ѭ��������Ҫ�����ڲ����棬��һ���в�ѯ
            IQueryFilter queryFilter = new QueryFilterClass();
            //Search�����������ֵ�Ļ�����SubFields�����Ч��
            queryFilter.SubFields = "Shape";
            IFeatureCursor cursor = polygonFeatureLayer.Search(queryFilter, true);
            IFeature polygonFeature = null;
            while ((polygonFeature = cursor.NextFeature()) != null)
            {
                IGeometry queryGeometry = polygonFeature.Shape;
                //�����ռ��ѯ
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = queryGeometry;
                spatialFilter.GeometryField = pointFeatureLayer.FeatureClass.ShapeFieldName;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                pointFeatureSelection.SelectFeatures(spatialFilter as IQueryFilter, esriSelectionResultEnum.esriSelectionResultAdd, false);
               
            }
            int count = pointFeatureSelection.SelectionSet.Count;
            axMapControl1.Refresh();
            //�ͷ��α�
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cursor);
            myWatch.Stop();
            string time = myWatch.Elapsed.TotalSeconds.ToString();
            MessageBox.Show("The selected point count is " + count.ToString() + "! and " + time + " Seconds");
        }

        private void iSpatialFilterSpatialCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch myWatch = Stopwatch.StartNew();
            //��MapControl�л�ȡ�ĵ�ͼ��
            IFeatureLayer pointFeatureLayer = axMapControl1.get_Layer(0) as IFeatureLayer;
            IFeatureSelection pointFeatureSelection = pointFeatureLayer as IFeatureSelection;
            //��MapControl�л�ȡ����ͼ��
            IFeatureLayer polygonFeatureLayer = axMapControl1.get_Layer(1) as IFeatureLayer;
            //���Spatial Cache
            ISpatialCacheManager spatialCacheManager = (ISpatialCacheManager)(pointFeatureLayer as IDataset).Workspace;
            IEnvelope cacheExtent = (pointFeatureLayer as IGeoDataset).Extent;
            //����Ƿ���ڻ���
            if (!spatialCacheManager.CacheIsFull)
            {
                //�����ڣ��򴴽�����
                spatialCacheManager.FillCache(cacheExtent);
            }

            //�����������в�ѯ
            IQueryFilter queryFilter = new QueryFilterClass();
            //Search�����������ֵ�Ļ�����SubFields�����Ч��
            queryFilter.SubFields = "Shape";
            IFeatureCursor cursor = polygonFeatureLayer.Search(queryFilter, true);
            IFeature polygonFeature = null;
            while ((polygonFeature = cursor.NextFeature()) != null)
            {
                IGeometry queryGeometry = polygonFeature.Shape;
                //�����ռ��ѯ
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = queryGeometry;
                spatialFilter.GeometryField = pointFeatureLayer.FeatureClass.ShapeFieldName;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                //ѡ��Ļ����Բ�����SubFields               
                pointFeatureSelection.SelectFeatures(spatialFilter as IQueryFilter, esriSelectionResultEnum.esriSelectionResultAdd, false);
                
            }
            int count = pointFeatureSelection.SelectionSet.Count;
            //��տռ仺��
            spatialCacheManager.EmptyCache();            
            axMapControl1.Refresh();
            //�ͷ��α�
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cursor);
            myWatch.Stop();
            string time = myWatch.Elapsed.TotalSeconds.ToString();
            MessageBox.Show("The selected point count is " + count.ToString() + "! and " + time + " Seconds");
        }

        private void iSpatialFilterGeometryBagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch myWatch = Stopwatch.StartNew();

            //��MapControl�л�ȡ�ĵ�ͼ��
            IFeatureLayer pointFeatureLayer = axMapControl1.get_Layer(0) as IFeatureLayer;
            IFeatureSelection pointFeatureSelection = pointFeatureLayer as IFeatureSelection;
            //��MapControl�л�ȡ����ͼ��
            IFeatureLayer polygonFeatureLayer = axMapControl1.get_Layer(1) as IFeatureLayer;

            //����GeometryBag
            IGeometryBag geometryBag = new GeometryBagClass();
            IGeometryCollection geometryCollection = (IGeometryCollection)geometryBag;
            IGeoDataset geoDataset = (IGeoDataset)polygonFeatureLayer;
            ISpatialReference spatialReference = geoDataset.SpatialReference;
            //һ��Ҫ��GeometryBag���ռ�ο�
            geometryBag.SpatialReference = spatialReference;

            IQueryFilter queryFilter = new QueryFilterClass();
            //Search�����������ֵ�Ļ�����SubFields�����Ч��
            queryFilter.SubFields = "Shape";
           
            //������Ҫ���࣬��һ��ȡGeometry����ӵ�GeometryBag��
            IFeatureCursor cursor = polygonFeatureLayer.Search(queryFilter, true);

            IFeature polygonFeature = null;
            while ((polygonFeature = cursor.NextFeature()) != null)
            {
                geometryCollection.AddGeometry(polygonFeature.ShapeCopy);
            }
            //ΪGeometryBag���ɿռ������������Ч��
            ISpatialIndex spatialIndex = (ISpatialIndex)geometryBag;
            spatialIndex.AllowIndexing = true;
            spatialIndex.Invalidate();
            //�����ռ��ѯ
            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.Geometry = geometryBag;
            spatialFilter.GeometryField = pointFeatureLayer.FeatureClass.ShapeFieldName;
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
            //ѡ��Ļ����Բ�����SubFields
            pointFeatureSelection.SelectFeatures(spatialFilter as IQueryFilter, esriSelectionResultEnum.esriSelectionResultAdd, false);

            int count = pointFeatureSelection.SelectionSet.Count;

            axMapControl1.Refresh();
            //�ͷ��α�
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cursor);

            myWatch.Stop();
            string time = myWatch.Elapsed.TotalSeconds.ToString();
            MessageBox.Show("The selected point count is " + count.ToString() + "! and " + time + " Seconds");
        }

        private void iQueryByLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch myWatch = Stopwatch.StartNew();

            //��MapControl�л�ȡ�ĵ�ͼ��
            IFeatureLayer pointFeatureLayer = axMapControl1.get_Layer(0) as IFeatureLayer;
            IFeatureSelection pointFeatureSelection = pointFeatureLayer as IFeatureSelection;
            //��MapControl�л�ȡ����ͼ��
            IFeatureLayer polygonFeatureLayer = axMapControl1.get_Layer(1) as IFeatureLayer;
            //����QueryByLayer
            IQueryByLayer queryByLayer = new QueryByLayerClass();
            queryByLayer.FromLayer = pointFeatureLayer;
            queryByLayer.ByLayer = polygonFeatureLayer;
            queryByLayer.LayerSelectionMethod = esriLayerSelectionMethod.esriLayerSelectCompletelyWithin;
            //�ò�����Ҫ����
            queryByLayer.UseSelectedFeatures = false;
            ISelectionSet selectionSet = queryByLayer.Select();

            pointFeatureSelection.SelectionSet = selectionSet;
            int count = pointFeatureSelection.SelectionSet.Count;

            axMapControl1.Refresh();

            myWatch.Stop();
            string time = myWatch.Elapsed.TotalSeconds.ToString();
            MessageBox.Show("The selected point count is " + count.ToString() + "! and " + time + " Seconds");
        }

      
    }
}