using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sensitive
{
    public class AcNode
    {
        public AcNode(string _value, AcNode _parent)
        {
            value = _value;
            is_end = false;
            parent = _parent;
            nexts = new Dictionary<string, AcNode>();
        }

        public string value;                          // 节点value
        public bool is_end;                         // 是否是屏蔽词结束节点
        public int pattern_len;                     // 记录屏蔽词长度， 用于反推开始位置
        public bool check_english_word;             // 是否检查是否是完整单词，例如： 屏蔽词 `sm` 不能屏蔽 small 里面的sm

        public AcNode failed;                       // 失效节点
        public AcNode parent;                       // 父节点
        public Dictionary<string, AcNode> nexts;      // 子节点

    }

    struct AcPoint
    {
        public AcPoint(int _start, int _end)
        {
            start = _start;
            end = _end;
        }

        public int start;
        public int end;
    }
    class AcSensitive
    {
        public AcSensitive()
        {
            ac_root_ = new AcNode('\0'.ToString(), null);
        }

        public void AddPattern(string pattern, bool isCheckPinyin = false)
        {
            addpattern_internal(pattern, pattern.Length, false);
            if (!isCheckPinyin) return;
            if (!is_contains_chinese(pattern)) return;
            

            AddPinYinPattern(pattern);
        }

        public void AddPinYinPattern(string pattern)
        {
            // 每个字的拼音当成一个节点，可以过滤 `毛泽冬`, 如果`毛泽东`是关键词
            addpattern_internal(pattern, pattern.Length, true);
            // 把拼音当成普通英文， 可以过滤 `maozedong`
            var pinyin = NPinyin.Pinyin.GetPinyin(pattern);
            addpattern_internal(pinyin, pinyin.Length, false);
            // 把拼音当成普通英文， 可以过滤 `mao ze dong`
            pinyin = NPinyin.Pinyin.GetPinyin(pattern, false);
            addpattern_internal(pinyin, pinyin.Length, false);
        }

        public void addpattern_internal(string pattern, int pattern_len, bool is_pinyin)
        {
            if (pattern.Length == 0) return;
            pattern = pattern.ToLower();
            pattern = pattern.Trim();                   // 去掉前后空白
            pattern = AcUtils.get().ToSimplified(pattern);    // 所有繁体转为简体

            // 初始堆
            var curNode = ac_root_;
            foreach(var c in pattern)
            {
                var key = is_pinyin ? NPinyin.Pinyin.GetPinyin(c) : c.ToString();
                if (curNode.nexts.ContainsKey(key))
                {
                    curNode = curNode.nexts[key];
                } else
                {
                    var oldNode = curNode;
                    curNode = new AcNode(key, oldNode);
                    oldNode.nexts[key] = curNode;
                }
            }
            curNode.is_end = true;
            curNode.check_english_word = !is_pinyin && is_full_english(pattern);
            curNode.pattern_len = pattern_len;
        }

        public void Build()
        {
            Queue<AcNode> queue = new Queue<AcNode>(); // 因为需要广度遍历，所以需要个队列记录子节点

            Action<AcNode> AddChildsToQueue = (node) => { foreach (var child in node.nexts) { queue.Enqueue(child.Value); }};
            // 初始化第一层子节点的失效节点位根节点
            ac_root_.failed = ac_root_;
            foreach(var child in ac_root_.nexts)
            {
                child.Value.failed = ac_root_;
                AddChildsToQueue(child.Value);
            }

            while(queue.Count != 0)
            {
                var node = queue.Dequeue();
                AddChildsToQueue(node);

                var parent = node.parent;
                while (parent != null)
                {
                    // 从父节点失效节点的子节点中查找 q节点
                    if (parent.failed.nexts.ContainsKey(node.value))
                    {
                        // 设置失效节点
                        node.failed = parent.failed.nexts[node.value];
                        parent = null;
                    } 
                    else
                    {
                        // 子节点没有
                        if (parent.failed == ac_root_)
                        {
                            // 父节点已经是根节点，失效节点只想根节点，结束查找
                            node.failed = ac_root_;
                            parent = null;
                        } else
                        {
                            // 继续线上查找
                            parent = parent.failed;
                        }
                    }
                }
            }
        }

        public bool Check(string src, bool isCheckPinyin)
        {
            var lower = src.ToLower();
            if (check_internal(lower, false)) return true;
            if (!isCheckPinyin) return false;
            if (!is_contains_chinese(src)) return false;

            return check_internal(lower, true);
        }

        public List<AcPoint> Search(string src, bool isCheckPinyin)
        {
            var lower = src.ToLower();
            lower = AcUtils.get().ToSimplified(lower);
            var ret = search_internal(lower, false);
            if (!isCheckPinyin) return ret;
            if (!is_contains_chinese(lower)) return ret;

            var retPinyin = search_internal(lower, true);
            if (ret.Count > 0 && retPinyin.Count > 0)
            {
                ret = ret.Union(retPinyin).ToList();
            }
            else
            {
                ret.AddRange(retPinyin);
            }
            return ret;
        }

        public string Filter(string src, string replace, bool isCheckPinyin)
        {
            var points = Search(src, isCheckPinyin);
            if (points.Count == 0) return src;

            string ret = "";
            int start = 0;
            foreach (var p in points)
            {
                ret = ret + src.Substring(start, p.start - start) + replace;
                start = p.end;
            }
            ret += src.Substring(start);
            return ret;
        }

        private List<AcPoint> search_internal(string src, bool is_pinyin)
        {
            int index = 0;
            var ret = new List<AcPoint>();

            Func<AcNode, AcNode> check_all_failed_nodes = failed => {
                while (failed != null && failed != ac_root_)
                {
                    if (failed.is_end && 
                    (!failed.check_english_word || 
                        is_really_end(src, index - failed.pattern_len, index)))
                    {
                        ret.Add(new AcPoint(index - failed.pattern_len, index));
                    }
                    failed = failed.failed;
                }
                return failed; 
            };

            var cur = ac_root_;
            foreach (var c in src)
            {
                index++;
            ReCheckTag:
                var key = is_pinyin ? NPinyin.Pinyin.GetPinyin(c) : c.ToString();
                if (cur.nexts.ContainsKey(key))
                {
                    // 找到子节点, 检查此节点所有失效节点链路是否有标识为结束节点的
                    // 如果链路中有结束节点，则结束查找，
                    check_all_failed_nodes(cur.failed);
                    cur = cur.nexts[key];
                }
                else
                {
                    // 未找到
                    if (cur == ac_root_) {
                        continue; // 已经检查到根节点了，继续检查下个字符
                    }

                    cur = cur.failed;
                    goto ReCheckTag;
                }

                if (cur.is_end && (!cur.check_english_word || is_really_end(src, index - cur.pattern_len, index)))
                {

                    ret.Add(new AcPoint(index - cur.pattern_len, index));
                }
            }
            return ret;
        }



        private bool check_internal(string src, bool is_pinyin)
        {
            var cur = ac_root_;
            foreach (var c in src)
            {
            ReCheckTag:
                var key = is_pinyin ? NPinyin.Pinyin.GetPinyin(c) : c.ToString();
                if (cur.nexts.ContainsKey(key))
                {
                    // 找到子节点, 检查此节点所有失效节点链路是否有标识为结束节点的
                    // 如果链路中有结束节点，则结束查找，
                    var failed = cur.failed;
                    while (failed != null && failed != ac_root_)
                    {
                        if (failed.is_end)
                        {
                            return true;
                        }
                        failed = failed.failed;
                    }
                    cur = cur.nexts[key];
                } else
                {
                    // 未找到
                    if (cur == ac_root_)
                        continue; // 已经检查到根节点了，继续检查下个字符

                    cur = cur.failed;
                    goto ReCheckTag;
                }

                if (cur.is_end)
                {
                    return true;
                }
            }
            return false;
        }

        private bool is_contains_chinese(string src)
        {
            return Regex.IsMatch(src, @"[\u4e00-\u9fa5]");
        }

        private bool is_full_english(string src)
        {
            return Regex.IsMatch(src, @"^[A-Za-z]+$");
        }

        private bool is_really_end(string src, int start, int end)
        {
            if (start > 0)
            {
                if (Char.IsLetter(src, start - 1))
                {
                    return false;
                }
            }
            if (end < src.Length)
            {
                if (Char.IsLetter(src, end))
                {
                    return false;
                }
            }
            return true;
        }

        public AcNode ac_root_;
    }
}
