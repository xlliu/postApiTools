﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace postApiTools.lib
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// 委托刷新树
    /// </summary>
    /// <param name="t"></param>
    /// <param name="image"></param>
    public delegate void showTreeview(TreeView t, ImageList image);
    /// <summary>
    /// 更新服务
    /// </summary>
    public class pUpdateServerWeb
    {
        /// <summary>
        /// 输出错误
        /// </summary>
        public static string error = "";

        /// <summary>
        /// sqlite
        /// </summary>
        private static pSqlite sqlite = new pSqlite(Config.historyDataDb);
        /// <summary>
        /// 树
        /// </summary>
        public static TreeView t = null;
        /// <summary>
        /// 树 图片
        /// </summary>
        public static ImageList image = null;
        /// <summary>
        /// pull按钮
        /// </summary>
        public static Button buttonPull = null;
        /// <summary>
        /// 判断线程是否完成结束
        /// </summary>
        private static int startEnd = 0;

        /// <summary>
        /// 总拉取
        /// </summary>
        public static void pull()
        {
            pullProjectMainTh();
        }
        /// <summary>
        /// 更新主栏目线程
        /// </summary>
        public static void pullProjectMainTh()
        {
            Thread th = new Thread(pullProjectMain);
            th.Start();
        }
        /// <summary>
        /// 更新单个
        /// </summary>
        /// <param name="hash"></param>
        public static void pullProjecrOne(string hash)
        {
            Thread th = new Thread(pullProjectOne);
            th.Start(hash);

        }
        /// <summary>
        /// 更新主栏目
        /// </summary>
        /// <returns></returns>
        public static void pullProjectOne(object obj)
        {
            try
            {
                string hash = (string)obj;
                if (startEnd > 0)
                {
                    return;
                }
                startEnd++;
                Dictionary<string, object> nowList = new Dictionary<string, object> { };
                Dictionary<int, object> d = sqlite.getRows(string.Format("select *from {0} where hash='{1}'", pForm1TreeView.getTableSetting(), hash));//本地列表
                if (d.Count <= 0)
                {
                    Form1.f.TextShowlogs("更新内容不存在!", "error");
                    return;
                }
                Dictionary<string, string> serverHashTemp = (Dictionary<string, string>)d[0];
                JObject job = pApizlHttp.getUserProjectOne(Config.userToken, serverHashTemp["server_hash"]);//获取指定列表
                if (job == null)
                {
                    error = pApizlHttp.error;
                    return;
                }
                JArray serverList = (JArray)job["result"];//获取结果
                //if (serverList.Count < d.Count)
                //{
                //    error = "选择提交..";
                //    return;
                //}
                for (int i = 0; i < serverList.Count; i++)
                {
                    if (d.Count <= 0)
                    {
                        if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                        {
                            continue;
                        }
                        Dictionary<string, object> addTemp = new Dictionary<string, object> { };
                        addTemp.Add("type", "insert");
                        addTemp.Add("server", serverList[i]);
                        nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要新增
                    }
                    for (int g = 0; g < d.Count; g++)
                    {
                        Dictionary<string, object> addTemp = new Dictionary<string, object> { };
                        Dictionary<string, string> temp = (Dictionary<string, string>)d[g];
                        if (serverList[i]["name"].ToString() == temp["name"] && serverList[i]["hash"].ToString() == temp["server_hash"])
                        {
                            addTemp.Add("type", "ok");
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                nowList.Remove(serverList[i]["hash"].ToString());
                                nowList.Add(serverList[i]["hash"].ToString(), addTemp);//已存在
                            }
                        }
                        if (serverList[i]["name"].ToString() == temp["name"] && serverList[i]["hash"].ToString() != temp["server_hash"])
                        {
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                continue;
                            }
                            addTemp.Add("type", "update");
                            addTemp.Add("result", temp);
                            addTemp.Add("server", serverList[i]);
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                nowList.Remove(serverList[i]["hash"].ToString());
                                nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要更新
                            }
                        }
                        if (serverList[i]["name"].ToString() != temp["name"] && serverList[i]["hash"].ToString() != temp["server_hash"])
                        {
                            string serverHash = serverList[i]["hash"].ToString();
                            if (nowList.ContainsKey(serverHash))
                            {
                                nowList.Remove(serverHash);
                            }
                            addTemp.Add("type", "insert");
                            addTemp.Add("server", serverList[i]);
                            nowList.Remove(serverList[i]["hash"].ToString());
                            nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要新增
                        }
                    }
                }
                foreach (KeyValuePair<string, object> f in nowList)
                {
                    Dictionary<string, object> t = (Dictionary<string, object>)f.Value;
                    if (t["type"].ToString() == "update")
                    {
                        Dictionary<string, string> resultUpdate = (Dictionary<string, string>)t["result"];
                        JObject serverUpdate = (JObject)t["server"];
                        pForm1TreeView.updateProjectServerHash(resultUpdate["hash"].ToString(), serverUpdate["hash"].ToString());//更新本地server hash
                    }
                    if (t["type"].ToString() == "insert")
                    {
                        JObject serverUpdate = (JObject)t["server"];
                        pForm1TreeView.insertMainSql(serverUpdate["name"].ToString(), serverUpdate["desc"].ToString(), serverUpdate["hash"].ToString());//新增项目}
                        //pForm1TreeView.insertMain(serverUpdate["name"].ToString(), serverUpdate                    ;//新增项目}
                    }
                }
                startEnd--;
                pullProjectPidList();//递归创建子类
                pullDocumentListTh();//创建文档
                return;
            }
            catch (Exception ex)
            {
                pLogs.logs(ex.ToString()); startEnd--;
                return;
            }
        }


        /// <summary>
        /// 更新主栏目
        /// </summary>
        /// <returns></returns>
        public static void pullProjectMain()
        {
            try
            {
                if (startEnd > 0)
                {
                    return;
                }
                startEnd++;
                Dictionary<string, object> nowList = new Dictionary<string, object> { };
                Dictionary<int, object> d = sqlite.getRows(string.Format("select *from {0} where pid=0", pForm1TreeView.getTableSetting()));//本地列表
                JObject job = pApizlHttp.getUserProjectList(Config.userToken);//获取服务器列表
                if (job == null)
                {
                    error = pApizlHttp.error;
                    return;
                }
                JArray serverList = (JArray)job["result"];//获取结果
                //if (serverList.Count < d.Count)
                //{
                //    error = "选择提交..";
                //    return;
                //}
                for (int i = 0; i < serverList.Count; i++)
                {
                    if (d.Count <= 0)
                    {
                        if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                        {
                            continue;
                        }
                        Dictionary<string, object> addTemp = new Dictionary<string, object> { };
                        addTemp.Add("type", "insert");
                        addTemp.Add("server", serverList[i]);
                        nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要新增
                    }
                    for (int g = 0; g < d.Count; g++)
                    {
                        Dictionary<string, object> addTemp = new Dictionary<string, object> { };
                        Dictionary<string, string> temp = (Dictionary<string, string>)d[g];
                        if (serverList[i]["name"].ToString() == temp["name"] && serverList[i]["hash"].ToString() == temp["server_hash"])
                        {
                            addTemp.Add("type", "ok");
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                nowList.Remove(serverList[i]["hash"].ToString());
                                nowList.Add(serverList[i]["hash"].ToString(), addTemp);//已存在
                            }
                        }
                        if (serverList[i]["name"].ToString() == temp["name"] && serverList[i]["hash"].ToString() != temp["server_hash"])
                        {
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                continue;
                            }
                            addTemp.Add("type", "update");
                            addTemp.Add("result", temp);
                            addTemp.Add("server", serverList[i]);
                            if (nowList.ContainsKey(serverList[i]["hash"].ToString()))
                            {
                                nowList.Remove(serverList[i]["hash"].ToString());
                                nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要更新
                            }
                        }
                        if (serverList[i]["name"].ToString() != temp["name"] && serverList[i]["hash"].ToString() != temp["server_hash"])
                        {
                            string serverHash = serverList[i]["hash"].ToString();
                            if (nowList.ContainsKey(serverHash))
                            {
                                nowList.Remove(serverHash);
                            }
                            addTemp.Add("type", "insert");
                            addTemp.Add("server", serverList[i]);
                            nowList.Remove(serverList[i]["hash"].ToString());
                            nowList.Add(serverList[i]["hash"].ToString(), addTemp);//需要新增
                        }
                    }
                }
                foreach (KeyValuePair<string, object> f in nowList)
                {
                    Dictionary<string, object> t = (Dictionary<string, object>)f.Value;
                    if (t["type"].ToString() == "update")
                    {
                        Dictionary<string, string> resultUpdate = (Dictionary<string, string>)t["result"];
                        JObject serverUpdate = (JObject)t["server"];
                        pForm1TreeView.updateProjectServerHash(resultUpdate["hash"].ToString(), serverUpdate["hash"].ToString());//更新本地server hash
                    }
                    if (t["type"].ToString() == "insert")
                    {
                        JObject serverUpdate = (JObject)t["server"];
                        pForm1TreeView.insertMainSql(serverUpdate["name"].ToString(), serverUpdate["desc"].ToString(), serverUpdate["hash"].ToString());//新增项目}
                        //pForm1TreeView.insertMain(serverUpdate["name"].ToString(), serverUpdate                    ;//新增项目}
                    }
                }
                startEnd--;
                pullProjectPidList();//递归创建子类
                pullDocumentListTh();//创建文档
                return;
            }
            catch (Exception ex)
            {
                buttonPull.Enabled = true;
                pLogs.logs(ex.ToString()); startEnd--; return;
            }
        }

        /// <summary>
        /// 更新子类列表线程
        /// </summary>
        public static void pullProjectPidListTh(string pid = "0")
        {
            Thread th = new Thread(pullProjectPidList);
            th.Start(pid);
        }
        /// <summary>
        /// 更新子类列表
        /// </summary>
        /// <returns></returns>
        public static void pullProjectPidList(object pid = null)
        {
            if (pid == null)
            {
                pid = "0";
            }
            Dictionary<int, object> d = sqlite.getRows(string.Format("select *from {0} where pid='{1}'", pForm1TreeView.getTableSetting(), pid));
            if (d.Count <= 0)
            {
                error = "没有项目";
                return;
            }
            for (int i = 0; i < d.Count; i++)
            {
                Dictionary<string, string> temp = (Dictionary<string, string>)d[i];
                JObject job = pApizlHttp.getProjectPidList(temp["server_hash"]);
                if (job != null)
                {
                    JArray jar = (JArray)job["result"];
                    pForm1TreeView.createProjectPidList(jar, temp["hash"]);
                    pullProjectPidList(temp["hash"]);//递归
                }
            }
            return;
        }
        /// <summary>
        /// 更新文章列表线程
        /// </summary>
        public static void pullDocumentListTh()
        {
            Thread th = new Thread(pullDocumentList);
            th.Start();
        }
        /// <summary>
        /// 更新文章列表
        /// </summary>
        /// <returns></returns>
        public static void pullDocumentList()
        {
            Dictionary<int, object> d = sqlite.getRows(string.Format("select *from {0}", pForm1TreeView.getTableSetting()));
            if (d.Count <= 0)
            {
                error = "没有需要更新";
                return;
            }
            for (int i = 0; i < d.Count; i++)
            {
                Dictionary<string, string> temp = (Dictionary<string, string>)d[i];
                JObject job = pApizlHttp.getDocumentList(temp["server_hash"]);
                if (job != null)
                {
                    JArray jar = (JArray)job["result"];
                    pForm1TreeView.createDocumentList(jar, temp["hash"]);
                }
            }
            pForm1TreeView.showMainData(t, image);
            buttonPull.Enabled = true;
            return;
        }
        /// <summary>
        /// 更新主栏目
        /// </summary>
        /// <returns></returns>
        public static bool updateProjectMain()
        {
            if (startEnd > 0)
            {
                return false;
            }
            startEnd++;
            Dictionary<int, object> nowList = new Dictionary<int, object> { };
            Dictionary<int, object> noneList = new Dictionary<int, object> { };
            Dictionary<int, object> updateList = new Dictionary<int, object> { };
            int noneI = 0;
            int nowListI = 0;
            Dictionary<int, object> d = sqlite.getRows(string.Format("select *from {0} where pid=0", pForm1TreeView.getTableSetting()));//本地列表
            JObject job = pApizlHttp.getUserProjectList(Config.userToken);//获取服务器列表
            if (job == null)
            {
                error = pApizlHttp.error;
                return false;
            }
            JArray serverList = (JArray)job["result"];//获取结果
            for (int i = 0; i < d.Count; i++)
            {
                Dictionary<string, string> result = (Dictionary<string, string>)d[i];
                if (result == null)
                {
                    continue;
                }
                for (int g = 0; g < serverList.Count; g++)
                {
                    if (serverList[i]["name"].ToString() != result["name"].ToString())
                    {
                        noneList.Add(noneI, result);
                        noneI++;
                    }
                    else if (serverList[i]["name"].ToString() == result["name"].ToString())
                    {
                        if (serverList[i]["hash"].ToString() != result["server_hash"].ToString())//判断本地是否不存在
                        {
                            pForm1TreeView.updateProjectServerHash(result["hash"].ToString(), serverList[i]["hash"].ToString());//更新本地server hash
                        }
                        else
                        { //真正存在
                            nowList.Add(nowListI, serverList[i]["hash"]);
                            nowListI++;
                        }
                        Thread th = new Thread(new ParameterizedThreadStart(updateProjectPid));//开启线程更新
                        th.Start(serverList[i]);
                    }
                }
            }
            for (int i = 0; i < noneList.Count; i++)//创建本地项目
            {
                Dictionary<string, string> result = (Dictionary<string, string>)noneList[i];
                //pForm1TreeView.insertMain(result["name"], result["desc"]);
                for (int g = 0; g < serverList.Count; g++)
                {
                    if (serverList[i]["name"].ToString() == result["name"])
                    {
                        if (serverList[i]["hash"].ToString() != result["server_hash"].ToString())//判断本地是否不存在
                        {
                            pForm1TreeView.updateProjectServerHash(result["hash"].ToString(), serverList[i]["hash"].ToString());//更新本地server hash
                        }
                        Thread th = new Thread(new ParameterizedThreadStart(updateProjectPid));//开启线程更新
                        th.Start(serverList[i]);
                    }
                }
            }
            startEnd--;
            return true;
        }


        /// <summary>
        /// 更新子栏目
        /// </summary>
        /// <returns></returns>
        public static void updateProjectPid(object data)
        {
            startEnd++;
            JObject d = (JObject)data;//服务器上hash
            if (d == null) { return; }
            Dictionary<string, string> nowHash = sqlite.getOne(string.Format("select *from {0} where server_hash='{1}'", pForm1TreeView.getTableSetting(), d["hash"]));//进行验证
            if (nowHash == null) { return; }
            JObject job = pApizlHttp.getProjectSettingPidList(Config.userToken, nowHash["server_hash"]);//获取服务器上子栏目
            if (job == null) { return; }
            Dictionary<int, object> list = sqlite.getRows(string.Format("select *from {0} where pid='{1}'", pForm1TreeView.getTableSetting(), nowHash["server_hash"]));//本地子项目列表
            if (list == null)//为空情况
            {
                for (int i = 0; i < job.Count; i++)
                {
                    pForm1TreeView.insertPidData(job[i]["name"].ToString(), lib.pBase.getHash(), nowHash["hash"], job[i]["hash"].ToString());//添加本地子类
                }
            }
            else
            {
                Dictionary<int, object> noneList = new Dictionary<int, object> { };
                Dictionary<int, object> updateList = new Dictionary<int, object> { };
                int noneListI = 0;
                int updateListI = 0;
                for (int i = 0; i < job.Count; i++)
                {
                    for (int g = 0; g < list.Count; g++)
                    {
                        Dictionary<string, string> tmep = (Dictionary<string, string>)list[g];
                        if (tmep["name"].ToString() == job[i]["name"].ToString())
                        {
                            if (tmep["server_hash"].ToString() != job[i]["hash"].ToString())//不存在serverhash 加入更新序列
                            {
                                Dictionary<string, string> updateListTemp = new Dictionary<string, string> { };
                                updateListTemp.Add("hash", tmep["hash"].ToString());
                                updateListTemp.Add("server_hash", job[i]["hash"].ToString());
                                updateList.Add(updateListI, updateList);
                                updateListI++;
                            }
                        }
                        else
                        {
                            Dictionary<string, string> noneListTemp = new Dictionary<string, string> { };
                            noneListTemp.Add("server_hash", job[i]["hash"].ToString());
                            noneListTemp.Add("project_hash", tmep["hash"].ToString());
                            noneListTemp.Add("name", job[i]["name"].ToString());
                            noneList.Add(noneListI, noneList);
                            noneListI++;
                        }
                    }
                }
                for (int i = 0; i < noneList.Count; i++) //检测出不包含
                {
                    Dictionary<string, string> noneListTemp = (Dictionary<string, string>)noneList[i];
                    pForm1TreeView.insertPidData(noneListTemp["name"], lib.pBase.getHash(), noneListTemp["project_hash"], noneListTemp["server_hash"]);//添加本地子类
                }
                for (int i = 0; i < updateList.Count; i++)//检测出需要更新
                {
                    Dictionary<string, string> updateListTemp = (Dictionary<string, string>)noneList[i];

                    pForm1TreeView.updateProjectPidServerHash(updateListTemp["hash"], updateListTemp["server_hash"]);//更新本地serverhash
                }
            }
            Dictionary<int, object> listNew = sqlite.getRows(string.Format("select *from {0} where pid='{1}'", pForm1TreeView.getTableSetting(), nowHash["server_hash"]));//本地子项目列表
            for (int i = 0; i < listNew.Count; i++)
            {
                Thread th = new Thread(new ParameterizedThreadStart(updateDocumentList));//开启线程更新
                th.Start(listNew[i]);
            }
            startEnd--;
        }

        /// <summary>
        /// 更新内容
        /// </summary>
        /// <returns></returns>
        public static void updateDocumentList(object data)
        {
            startEnd++;
            Dictionary<int, object> noneList = new Dictionary<int, object> { };
            Dictionary<int, object> updateList = new Dictionary<int, object> { };
            int noneListI = 0;
            int updateListI = 0;
            Dictionary<string, string> d = (Dictionary<string, string>)data;
            if (d == null) { return; }
            string serverHash = d["server_hash"];
            Dictionary<int, object> rows = sqlite.getRows(string.Format("select *from {0} where project_id ='{1}'", pForm1TreeView.getTable(), d["hash"]));//获取栏目下文章列表
            JObject job = pApizlHttp.getDocumentList(Config.userToken, serverHash);//获取线上
            JArray serverRows = (JArray)job["result"];
            if (rows == null || rows.Count <= 0)
            {
                for (int i = 0; i < serverRows.Count; i++)
                {
                    pForm1TreeView.addApiSql(lib.pBase.getHash(), d["hash"], serverRows[i]["name"].ToString(), serverRows[i]["desc"].ToString(), serverRows[i]["url"].ToString(), serverRows[i]["urldata"].ToString(), serverRows[i]["method"].ToString(), serverRows[i]["hash"].ToString());//添加文档到本地
                }
            }
            for (int i = 0; i < serverRows.Count; i++)
            {
                for (int g = 0; g < rows.Count; g++)
                {
                    Dictionary<string, string> temp = (Dictionary<string, string>)rows[i];
                    if (temp["name"] == serverRows[i]["name"].ToString())
                    {
                        if (temp["server_hash"] != serverRows[i]["hash"].ToString()) //不存在serverhash
                        {
                            Dictionary<string, string> updateListTemp = new Dictionary<string, string> { };
                            updateListTemp.Add("hash", temp["hash"]);
                            updateListTemp.Add("server_hash", serverRows[i]["hash"].ToString());
                            updateList.Add(updateListI, updateListTemp);
                            updateListI++;
                        }
                    }
                    else
                    {//完全不存在数据
                        Dictionary<string, string> noneListTemp = new Dictionary<string, string> { };
                        noneListTemp.Add("hash", serverRows[i]["hash"].ToString());
                        noneListTemp.Add("method", serverRows[i]["method"].ToString());
                        noneListTemp.Add("urldata", serverRows[i]["urldata"].ToString());
                        noneListTemp.Add("url", serverRows[i]["url"].ToString());
                        noneListTemp.Add("desc", serverRows[i]["desc"].ToString());
                        noneListTemp.Add("name", serverRows[i]["name"].ToString());
                        noneList.Add(noneListI, noneListTemp);
                        noneListI++;
                    }
                }
            }
            for (int i = 0; i < noneList.Count; i++)
            {
                Dictionary<string, string> noneListTemp = (Dictionary<string, string>)noneList[i];
                pForm1TreeView.addApiSql(lib.pBase.getHash(), d["hash"], noneListTemp["name"].ToString(), noneListTemp["desc"].ToString(), noneListTemp["url"].ToString(), noneListTemp["urldata"].ToString(), noneListTemp["method"].ToString(), noneListTemp["hash"].ToString());//添加文档到本地
            }
            startEnd--;
        }



    }
}
