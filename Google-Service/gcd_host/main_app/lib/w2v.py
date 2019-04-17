import re
import xml.etree.ElementTree as ET
from pprint import pprint
from gensim import corpora, models

class JavaContentModel:
    default_w2v_source = 'java-content-selected.txt'
    default_stopwords = 'stopwords.txt'
    default_sub_regex = r'[{}\d\\<>/\[\]\'.,;()|%\-:="*?]'
    default_topic_list = 'raw_topic.txt'

    m_dictionary = None
    m_w2v = None
    m_topic_list = None

    def __init__(self):
        with open(self.default_topic_list, 'r') as fin:
            self.m_topic_list = [i.lower().strip() for i in fin.readlines()]

    def query_topics_from_raw(self, terms, out_topn=5, term_topn=5):
        rt = []
        terms = terms + self.query_similar_terms(terms, topn=term_topn)
        for i in terms:
            rt.append(self.query_topics(i))
        return [i for j in rt for i in j][:out_topn]

    def query_topics(self, cleaned_term):
        found_topics = list(filter(lambda x: cleaned_term in x, self.m_topic_list))
        return found_topics

    def query_similar_terms(self, terms, topn=10):
        filtered_terms = [i for i in terms if i in self.m_w2v.wv]
        if filtered_terms:
            return [i for i, _ in self.m_w2v.wv.most_similar(filtered_terms, topn=topn)]
        return []

    def train_default_w2v(self):
        # Read the raw content

        w2v_source = self.default_w2v_source
        tree = ET.parse(w2v_source)
        root = tree.getroot()
        page_xml_tag = '{http://www.mediawiki.org/xml/export-0.10/}page'
        docs = []

        for child in root:
            if (child.tag == page_xml_tag):
                page_content = filter(lambda x: len(x.strip()) > 0, child.itertext())
                page_content = "".join(page_content)
                docs.append(page_content)
        
        # Preprocess and tokenize

        stopword_source = self.default_stopwords
        with open(stopword_source, 'r') as fin:
            stopwords = set([i.strip() for i in fin.readlines()])

        texts = [ self.doc_to_tokens(doc, stopwords) for doc in docs ]

        # Build a w2v model

        dictionary = corpora.Dictionary(texts)
        dictionary.save('./java-content.dict')
        print(dictionary)

        w2v_model = models.Word2Vec(texts, size=100, window=5, min_count=1, workers=4)
        w2v_model.save("w2c-java-filter.model")

        self.m_dictionary = dictionary
        self.m_w2v = w2v_model

    def doc_to_tokens(self, doc, stopwords):
        sub_regex = self.default_sub_regex
        _doc = doc
        _doc = _doc.lower().split()
        tokens = []
        for w in _doc:
            _w = w.strip()
            _w = re.sub(sub_regex, '', _w).replace('\\n', '').strip()
            if len(_w) > 0 and _w not in stopwords:
                tokens.append(_w)
        return tokens

if __name__ == '__main__':
    javaw2v = JavaContentModel()
    javaw2v.train_default_w2v()
    pprint(javaw2v.query_topics_from_raw(['while']))

