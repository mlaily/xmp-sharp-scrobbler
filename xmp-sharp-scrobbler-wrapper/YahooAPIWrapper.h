class YahooAPIWrapperPrivate;

class YahooAPIWrapper
{
    private: YahooAPIWrapperPrivate* _private;

    public: YahooAPIWrapper();
	
	public: ~YahooAPIWrapper();
    
    public: double GetBid(const char* symbol);

    public: double GetAsk(const char* symbol);
    
    public: const char* GetCapitalization(const char* symbol);
    
    public: const char** GetValues(const char* symbol, const char* fields);
};